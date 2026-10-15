# Interface references

Quiet PC has no bundled third-party libraries, fonts, images or executable tools.

Windows Core Audio declarations follow the Windows SDK interfaces:

- [IMMDeviceEnumerator](https://learn.microsoft.com/en-us/windows/win32/api/mmdeviceapi/nn-mmdeviceapi-immdeviceenumerator)
- [IAudioEndpointVolume](https://learn.microsoft.com/en-us/windows/win32/api/endpointvolume/nn-endpointvolume-iaudioendpointvolume)

The internal notification COM interface layout was adapted from the example published by Castorix31 in [How to Enable/Disable Windows Notification using C#?](https://learn.microsoft.com/en-us/answers/questions/2154912/how-to-enable-disable-windows-notification-using-c). The app uses its own C# wrapper and recovery logic. The interface is undocumented; the example is a compatibility reference, not a Microsoft support guarantee.

Brand assets and icon drawing code were created for this project and are covered by its MIT licence.
