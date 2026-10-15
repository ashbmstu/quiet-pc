using System;
using System.IO;
using System.IO.Pipes;
using System.Security.AccessControl;
using System.Security.Principal;
using System.Text;
using System.Threading;
using System.Web.Script.Serialization;

namespace QuietPC {
    public sealed class CommandRequest {
        public string Action { get; set; }
        public int Minutes { get; set; }
    }
    public sealed class CommandReply {
        public bool Ok { get; set; }
        public string Error { get; set; }
        public bool Quiet { get; set; }
        public string EndsAtUtc { get; set; }
        public int PendingAudioRestores { get; set; }
        public bool PendingNotificationRestore { get; set; }
    }
    sealed class CommandChannel : IDisposable {
        internal static readonly string Name = "QuietPC-" + WindowsIdentity.GetCurrent().User.Value + "-" + System.Diagnostics.Process.GetCurrentProcess().SessionId;
        readonly Func<CommandRequest, CommandReply> handle;
        readonly Action exitAfterReply;
        readonly Thread thread;
        volatile bool stopping;
        NamedPipeServerStream current;
        readonly object gate = new object();
        public CommandChannel(Func<CommandRequest, CommandReply> handle, Action exitAfterReply) {
            this.handle = handle;
            this.exitAfterReply = exitAfterReply;
            thread = new Thread(Listen) { IsBackground = true, Name = "Quiet PC commands" }; thread.Start();
        }
        void Listen() {
            while (!stopping) {
                try {
                    var security = new PipeSecurity();
                    security.SetAccessRuleProtection(true, false);
                    security.AddAccessRule(new PipeAccessRule(WindowsIdentity.GetCurrent().User, PipeAccessRights.FullControl, AccessControlType.Allow));
                    using (var pipe = new NamedPipeServerStream(Name, PipeDirection.InOut, 1, PipeTransmissionMode.Byte, PipeOptions.Asynchronous, 4096, 4096, security)) {
                        lock (gate) { if (stopping) return; current = pipe; }
                        pipe.WaitForConnection();
                        // A stalled client must not block all later commands indefinitely.
                        using (var deadline = new System.Threading.Timer(delegate { try { pipe.Dispose(); } catch { } }, null, 10000, Timeout.Infinite)) {
                            var reader = new StreamReader(pipe, Encoding.UTF8, false, 1024, true);
                            var writer = new StreamWriter(pipe, new UTF8Encoding(false), 1024, true) { AutoFlush = true };
                            var json = ReadLine(reader);
                            CommandReply reply;
                            CommandRequest request = null;
                            try {
                                request = new JavaScriptSerializer().Deserialize<CommandRequest>(json);
                                if (request == null) throw new ArgumentException("Empty command.");
                                reply = handle(request);
                            } catch (Exception e) { reply = new CommandReply { Ok = false, Error = e.Message }; }
                            writer.WriteLine(new JavaScriptSerializer().Serialize(reply));
                            if (reply.Ok && request != null && (request.Action == "--exit" || request.Action == "--handoff")) exitAfterReply();
                        }
                    }
                } catch (Exception e) { if (!stopping) Program.Log("Command channel: " + e.Message); }
                finally { lock (gate) current = null; }
                if (!stopping) Thread.Sleep(20);
            }
        }
        static string ReadLine(StreamReader reader) {
            var text = new StringBuilder();
            while (text.Length < 4096) {
                int value = reader.Read();
                if (value < 0) throw new EndOfStreamException("Incomplete command.");
                if (value == '\n') return text.ToString();
                text.Append((char)value);
            }
            throw new InvalidDataException("Command exceeds 4096 characters.");
        }
        public static CommandReply Send(CommandRequest request, int timeout) {
            using (var pipe = new NamedPipeClientStream(".", Name, PipeDirection.InOut, PipeOptions.Asynchronous)) {
                pipe.Connect(timeout);
                using (var deadline = new System.Threading.Timer(delegate { try { pipe.Dispose(); } catch { } }, null, timeout, Timeout.Infinite)) {
                    var writer = new StreamWriter(pipe, new UTF8Encoding(false), 1024, true) { AutoFlush = true };
                    var reader = new StreamReader(pipe, Encoding.UTF8, false, 1024, true);
                    writer.WriteLine(new JavaScriptSerializer().Serialize(request));
                    return new JavaScriptSerializer().Deserialize<CommandReply>(ReadLine(reader));
                }
            }
        }
        public void Dispose() { stopping = true; lock (gate) { if (current != null) current.Dispose(); } }
    }
}
