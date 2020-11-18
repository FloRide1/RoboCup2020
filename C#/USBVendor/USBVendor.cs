using EventArgsLibrary;
using Microsoft.Win32.SafeHandles;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Management;
using System.Runtime.ConstrainedExecution;
using System.Text;
using System.Threading;
using System.Threading.Tasks;

namespace USBVendor
{
    public class USBVendor
    {
        string Name = "";
        UInt32 totalByteReceived;
        private Boolean _deviceDetected;
        private Boolean _deviceReady;
        private SafeFileHandle _deviceHandle;
        private WinUsbCommunications.DeviceInfo _myDeviceInfo = new WinUsbCommunications.DeviceInfo();
        private readonly DeviceManagement _myDeviceManagement = new DeviceManagement();
        private readonly WinUsbCommunications _myWinUsbCommunications = new WinUsbCommunications();
        private ManagementEventWatcher _deviceArrivedWatcher;
        private ManagementEventWatcher _deviceRemovedWatcher;
        private Boolean _windows81;

        private WinUsbCommunications.SafeWinUsbHandle _winUsbHandle;
        List<Device> cmv8DeviceListeFound = new List<Device>();
        private const String DeviceInterfaceGuid = "{58D07210-27C1-11DD-BD0B-0800200C9a66}";
        private const String VendorID = "04D8";
        private const String ProductID = "0053";

        Thread UsbPipeReadTask;

        private enum WmiDeviceProperties
        {
            Caption,
            Description,
            Manufacturer,
            Name,
            CompatibleID, // Returns System.String[]
            PNPDeviceID,
            DeviceID,
            ClassGUID,
            Availability // Always returns zero.
        }
        ///  <summary>
        ///  Define a class of delegates with the same parameters as 
        ///  WinUsbDevice.SendViaBulkTransfer.
        ///  Used for asynchronous writes to the device.
        ///  </summary>		

        private delegate void SendToDeviceDelegate
            (WinUsbCommunications.SafeWinUsbHandle winUsbHandle,
            WinUsbCommunications.DeviceInfo myDevInfo,
            UInt32 bufferLength,
            Byte[] buffer,
            ref UInt32 lengthTransferred,
            ref Boolean success);

        ///  <summary>
        ///  Define a class of delegates with the same parameters as 
        ///  WinUsbDevice.ReadViaBulkTransfer.
        ///  Used for asynchronous reads from the device.
        ///  </summary>

        private delegate void ReceiveFromDeviceDelegate(WinUsbCommunications.SafeWinUsbHandle winUsbHandle, WinUsbCommunications.DeviceInfo myDeviceInfo, UInt32 bytesToRead, ref Byte[] dataBuffer, ref UInt32 bytesRead, ref Boolean success);
        private delegate void ReceiveFromDeviceDelegateIso(WinUsbCommunications.SafeWinUsbHandle winUsbHandle, WinUsbCommunications.DeviceInfo myDeviceInfo, UInt32 bytesToRead, ref Byte[] dataInBuffer, ref UInt32 bytesRead, UInt32 numberOfPackets, ref Boolean succcess);


        public USBVendor()
        {
            UsbPipeReadTask = new Thread(usbReadTask);      //On declare la tache de lecture usb

            Startup();
        }

        private void Startup()
        {
            try
            {
                var myWinUsbCommunications = new WinUsbCommunications();
                _winUsbHandle = new WinUsbCommunications.SafeWinUsbHandle();
                DetectWindows81();
                //InitializeDisplay();
                DeviceNotificationsStart();

                FindMyDevice();

                Thread.Sleep(1000);
                // Create message polling thread.
                if (!UsbPipeReadTask.IsAlive)
                {
                    UsbPipeReadTask = new Thread(usbReadTask);      //On declare la tache de lecture usb
                        UsbPipeReadTask.Start();
                }
            }
            catch (Exception ex)
            {
                DisplayException(Name, ex);
                throw;
            }
        }
        internal static void DisplayException(String name, Exception e)
        {
            try
            {
                //  Create an error message.

                String message = "Exception: " + e.Message + Environment.NewLine + "Module: " + name + Environment.NewLine + "Method: " + e.TargetSite.Name;

                const string caption = "Unexpected Exception";

                //MessageBox.Show(message, caption, MessageBoxButtons.OK);
                Console.Write(message);
            }
            catch (Exception ex)
            {
                DisplayException(name, ex);
                throw;
            }
        }

        UInt32 bytesToRead = Convert.ToUInt32(2048);//204810
        Byte[] dataBuffer = new Byte[2048];
        UInt32 bytesRead = 0;
        bool readSuccess = true;
        bool success = false;
        bool continueTasks = true;
        bool deviceResetted = false;
        float powerLevel;
        void usbReadTask()
        {
            try
            {
                while (continueTasks)
                {
                    //if(jasonDeviceListeFound.Count>0)
                    if (readSuccess)
                    {
                        if (cmv8DeviceListeFound.Count > 0)
                        {
                            readSuccess = false;
                            //RequestToReceiveDataViaBulkTransfer(cmv8DeviceListeFound[0], bytesToRead, dataBuffer, ref bytesRead, ref readSuccess);
                            //RequestToReceiveDataViaIsochronousTransfer(cmv8DeviceListeFound[0], bytesToRead,ref dataBuffer, ref bytesRead, ref readSuccess);
                            _myWinUsbCommunications.ReceiveDataViaBulkTransfer(cmv8DeviceListeFound[0]._winUsbHandle,
                            cmv8DeviceListeFound[0]._myDeviceInfo,
                             bytesToRead,
                             ref dataBuffer,
                             ref bytesRead,
                             ref readSuccess);
                            byte[] bufff = new byte[bytesRead];
                            for(int i=0; i<bytesRead;i++)
                            {
                                bufff[i] = dataBuffer[i];
                            }
                            OnUSBDataReceived(bufff);
                        //    ProcessUSBReceivedMessage(dataBuffer, totalByteReceived);
                            //rcvMessageQueue.Enqueue(dataBuffer);
                        }
                    }
                    else if (deviceResetted == true && _deviceReady)
                    {
                        readSuccess = false;
                        deviceResetted = false;
                        //RequestToReceiveDataViaBulkTransfer(cmv8DeviceListeFound[0], bytesToRead, dataBuffer, ref bytesRead, ref success);
                        _myWinUsbCommunications.ReceiveDataViaBulkTransfer(cmv8DeviceListeFound[0]._winUsbHandle,
                           cmv8DeviceListeFound[0]._myDeviceInfo,
                            bytesToRead,
                            ref dataBuffer,
                            ref bytesRead,
                            ref readSuccess);
                        //RequestToReceiveDataViaIsochronousTransfer(cmv8DeviceListeFound[0], bytesToRead,ref dataBuffer, ref bytesRead, ref readSuccess);
                    }
                    else
                    {
                        //RequestToReceiveDataViaBulkTransfer(cmv8DeviceListeFound[0], bytesToRead, dataBuffer, ref bytesRead, ref readSuccess);
                        //RequestToReceiveDataViaIsochronousTransfer(cmv8DeviceListeFound[0], bytesToRead, ref dataBuffer, ref bytesRead, ref readSuccess);
                        //Thread.Sleep(2);
                    }
                    Thread.Sleep(1);
                }
            }
            catch {
                ;
            };
        }

        //Input events
        public void SendUSBMessage(object sender, EventArgsLibrary.MessageEncodedArgs e)
        {
            if (_deviceDetected && _deviceReady)
            {
                UInt32 LengthTransferred = 0;
                Boolean success = false;
                if (e.Msg.Length <= 128 && cmv8DeviceListeFound.Count > 0)
                    RequestToSendDataViaBulkTransfer(cmv8DeviceListeFound[0], (uint)e.Msg.Length, e.Msg, ref LengthTransferred, ref success);
                else
                {
                    Int32 bytesToSend = e.Msg.Length;
                    while (bytesToSend > 0)
                    {
                        RequestToSendDataViaBulkTransfer(cmv8DeviceListeFound[0], (uint)e.Msg.Length, e.Msg, ref LengthTransferred, ref success);
                        bytesToSend -= (Int32)LengthTransferred;
                    }
                }
            }
        }
        //public delegate void DataReceivedEventHandler(object sender, DataReceivedArgs e);
        public event EventHandler<DataReceivedArgs> OnUSBDataReceivedEvent;
        public virtual void OnUSBDataReceived(byte[] data)
        {
            var handler = OnUSBDataReceivedEvent;
            if (handler != null)
            {
                handler(this, new DataReceivedArgs { Data = data });
            }
        }
        ///  <summary>
        ///  If a device with the specified device interface GUID hasn't been previously detected,
        ///  look for it. If found, open a handle to the device.
        ///  </summary>
        ///  
        ///  <returns>
        ///  True if the device is detected, False if not detected.
        ///  </returns>
        List<Device> deviceListeFound = new List<Device>();
        private void FindMyDevice()
        {
            try
            {
                const uint pipeTimeout = 20;

                if (!(_deviceReady))
                {
                    // Convert the device interface GUID String to a GUID object: 

                    var winUsbDemoGuid = new Guid(DeviceInterfaceGuid);

                    // Fill an array with the device path names of all attached devices with matching GUIDs.

                    //var deviceFound = _myDeviceManagement.FindDeviceFromGuid(winUsbDemoGuid, ref devicePathName);//Modif VB

                    var deviceFound = _myDeviceManagement.FindDeviceListeFromGuid(winUsbDemoGuid, ref deviceListeFound);

                    if (deviceFound)
                    {
                        foreach (Device _device in deviceListeFound)//MODIF VB le foreach n'existait pas avant
                        {
                            //_deviceHandle = _myWinUsbCommunications.GetDeviceHandle(devicePathName);
                            _device._deviceHandle = _myWinUsbCommunications.GetDeviceHandle(_device._devicePathName);

                            if (!_device._deviceHandle.IsInvalid)
                            {
                                _device._deviceReady = true;

                                _myWinUsbCommunications.InitializeDevice(_device._deviceHandle, ref _device._winUsbHandle, ref _device._myDeviceInfo, pipeTimeout);

                                DisplayDeviceSpeed(_device);
                            }
                            else
                            {
                                // There was a problem in retrieving the information.

                                _device._deviceReady = false;
                                _myWinUsbCommunications.CloseDeviceHandle(_device._deviceHandle, _device._winUsbHandle);
                                //LstResults.Items.Add("Device not found.");
                                //MyMarshalToForm(FormActions.AddItemToListBox.ToString(), "Device not found.");
                            }
                        }
                    }
                    else
                    {
                        //CmdSendandReceiveViaBulkTransfers.Enabled = true;
                        //CmdSendAndReceiveViaInterruptTransfers.Enabled = true;
                    }
                }
                else
                {
                    //LstResults.Items.Add("The device has been detected.");
                    Console.WriteLine( "The device has been detected.");
                    DisplayDeviceSpeed(null);
                }

                // Display device information.

                FindDeviceUsingWmi();
                MatchFoundDeviceUsingVidPid();
                //ScrollToBottomOfListBox();
                //MyMarshalToForm(FormActions.ScrollToBottomOfListBox.ToString(), "");
            }
            catch (Exception ex)
            {
                DisplayException(Name, ex);
                throw;
            }
        }


        ///  <summary>
        ///  Get and display the device's speed in the list box.
        ///  </summary>		

        private void DisplayDeviceSpeed(Device device)
        {
            try
            {
                var speed = "";

                if (device != null)
                {
                    WinUsbCommunications.QueryDeviceSpeed(device._winUsbHandle, ref device._myDeviceInfo);

                    switch (device._myDeviceInfo.DeviceSpeed)
                    {
                        case 1:
                            speed = "low or full speed";
                            break;
                        case 3:
                            speed = "high speed or SuperSpeed";
                            break;
                    }
                }

                //LstResults.Items.Add("Device is " + speed);   //Modif a decommenter 24/08/2015
                //MyMarshalToForm(FormActions.AddItemToListBox.ToString(), "Device is " + speed);
            }
            catch (Exception ex)
            {
                DisplayException(Name, ex);
                throw;
            }
        }

        ///  <summary>
        ///  Use WMI to find a device by Vendor ID and Product ID. If found, display device properties.
        ///  Can also search by ClassGUID, description, and other properties. 
        ///  The WMI functions don't detect the device interface GUID so you need to use another property or properties.
        ///  </summary>
        /// 
        private Boolean FindDeviceUsingWmi()
        {
            try
            {
                // ClassGUID value from Windows-provided winusb.inf for use if you want to search by ClassGUID.

                //const String classGuid = "88bae032-5a81-49f0-bc3d-a4ff138216d6";
                const String classGuid = "a503e2d3-a031-49dc-b684-c99085dbfe92";

                _deviceDetected = false;
                var searcher = new ManagementObjectSearcher("root\\CIMV2", "SELECT * FROM Win32_PnPEntity");

                foreach (ManagementObject queryObj in searcher.Get())
                {
                    // More query exampless for the device search below:

                    // Search for any WinUSB device:
                    // if (queryObj["ClassGUID"].ToString().Contains(classGuid))

                    // Search by description:
                    // if (queryObj["Description"].ToString().ToLower().Contains("winusb"))

                    // Prepend "@" in string below to treat backslash as a normal character (not escape character):

                    const String deviceIdString = @"USB\VID_" + VendorID + "&PID_" + ProductID;

                    if (queryObj["PNPDeviceID"].ToString().Contains(deviceIdString))
                    {
                        _deviceDetected = true;
                        //MyMarshalToForm(FormActions.AddItemToListBox.ToString(), "--------");
                        //MyMarshalToForm(FormActions.AddItemToListBox.ToString(), "My device found (WMI):");

                        foreach (WmiDeviceProperties wmiDeviceProperty in Enum.GetValues(typeof(WmiDeviceProperties)))
                        {
                            //MyMarshalToForm(FormActions.AddItemToListBox.ToString(), (wmiDeviceProperty.ToString() + ": " + queryObj[wmiDeviceProperty.ToString()]));
                            Console.WriteLine(wmiDeviceProperty.ToString() + ": {0}", queryObj[wmiDeviceProperty.ToString()]);
                        }
                        //MyMarshalToForm(FormActions.AddItemToListBox.ToString(), "--------");
                        //MyMarshalToForm(FormActions.ScrollToBottomOfListBox.ToString(), "");
                    }
                }
                if (!_deviceDetected)
                {
                    _deviceReady = false;
                    //MyMarshalToForm(FormActions.AddItemToListBox.ToString(), "My device not found (WMI)");
                    //MyMarshalToForm(FormActions.ScrollToBottomOfListBox.ToString(), "");
                }
                return _deviceDetected;
            }
            catch (Exception ex)
            {
                DisplayException(Name, ex);
                throw;
            }
        }

        private Boolean MatchFoundDeviceUsingVidPid()
        {
            try
            {
                _deviceDetected = false;
                var searcher = new ManagementObjectSearcher("root\\CIMV2", "SELECT * FROM Win32_PnPEntity");

                foreach (Device dev in deviceListeFound)
                {
                    if (dev._devicePathName.Contains("vid_" + VendorID.ToLower() + "&pid_" + ProductID.ToLower()))
                    {
                        dev._isCMV8Device = true;
                        _deviceDetected = true;
                        _deviceReady = true;
                        deviceResetted = true;
                        cmv8DeviceListeFound.Add(dev);
                    }
                }
                if (!_deviceDetected)
                {
                    _deviceReady = false;
                    //MyMarshalToForm(FormActions.AddItemToListBox.ToString(), "My device not found (WMI)");
                    //MyMarshalToForm(FormActions.ScrollToBottomOfListBox.ToString(), "");
                }
                return _deviceDetected;
            }
            catch (Exception ex)
            {
                DisplayException(Name, ex);
                throw;
            }
        }
        /// <summary>
        /// Detects if Windows 8.1 or later (to enable isochronous transfers). 
        /// Windows 8.1 = 6.3.9600.0
        /// Windows 8.0 = 6.2.9200.0
        /// </summary>

        void DetectWindows81()
        {
            String completeWindowsVersion = Convert.ToString(Environment.OSVersion.Version);
            var windowsVersion = Convert.ToDouble(completeWindowsVersion.Substring(0, 3).Replace('.', ','));

            if (windowsVersion >= (double)6.3)
            {
                _windows81 = true;
                //LstResults.Items.Add("Windows version is 8.1 or later; WinUSB isochronous transfers supported.");
            }
            else
            {
                _windows81 = false;
                //LstResults.Items.Add("Windows version is not 8.1 or later; WinUSB isochronous transfers not supported.");
            }
        }

        ///  <summary>
        ///  Add handlers to detect device arrival and removal.
        ///  </summary>

        private void DeviceNotificationsStart()
        {
            AddDeviceArrivedHandler();
            AddDeviceRemovedHandler();
        }

        ///  <summary>
        ///  Add a handler to detect arrival of devices.
        ///  </summary>

        private void AddDeviceArrivedHandler()
        {
            const Int32 pollingIntervalSeconds = 3;
            var scope = new ManagementScope("root\\CIMV2");
            scope.Options.EnablePrivileges = true;

            try
            {
                var q = new WqlEventQuery();
                q.EventClassName = "__InstanceCreationEvent";
                q.WithinInterval = new TimeSpan(0, 0, pollingIntervalSeconds);
                q.Condition = @"TargetInstance ISA 'Win32_USBControllerdevice'";
                _deviceArrivedWatcher = new ManagementEventWatcher(scope, q);
                _deviceArrivedWatcher.EventArrived += DeviceAdded;

                _deviceArrivedWatcher.Start();
            }
            catch (Exception e)
            {
                Console.WriteLine(e.Message);
                if (_deviceArrivedWatcher != null)
                    _deviceArrivedWatcher.Stop();
            }
        }
        ///  <summary>
        ///  Called on arrival of any device.
        ///  Calls a routine that searches to see if the desired device is present.
        ///  </summary>

        private void DeviceAdded(object sender, EventArrivedEventArgs e)
        {
            try
            {
                Console.WriteLine("A USB device has been inserted");

                FindMyDevice();
                //_deviceDetected = FindDeviceUsingWmi();
            }
            catch (Exception ex)
            {
                DisplayException(Name, ex);
                throw;
            }
        }

        ///  <summary>
        ///  Called on removal of any device.
        ///  Calls a routine that searches to see if the desired device is still present.
        ///  </summary>
        /// 
        private void DeviceRemoved(object sender, EventArgs e)
        {
            try
            {
                Console.WriteLine("A USB device has been removed");

                _deviceDetected = FindDeviceUsingWmi();
                cmv8DeviceListeFound.Clear();
            }
            catch (Exception ex)
            {
                DisplayException(Name, ex);
                throw;
            }
        }

        ///  <summary>
        ///  Add a handler to detect removal of devices.
        ///  </summary>

        private void AddDeviceRemovedHandler()
        {
            const Int32 pollingIntervalSeconds = 3;
            var scope = new ManagementScope("root\\CIMV2");
            scope.Options.EnablePrivileges = true;

            try
            {
                var q = new WqlEventQuery();
                q.EventClassName = "__InstanceDeletionEvent";
                q.WithinInterval = new TimeSpan(0, 0, pollingIntervalSeconds);
                q.Condition = @"TargetInstance ISA 'Win32_USBControllerdevice'";
                _deviceRemovedWatcher = new ManagementEventWatcher(scope, q);
                _deviceRemovedWatcher.EventArrived += DeviceRemoved;
                _deviceRemovedWatcher.Start();
            }
            catch (Exception e)
            {
                Console.WriteLine(e.Message);
                if (_deviceRemovedWatcher != null)
                    _deviceRemovedWatcher.Stop();
            }
        }
        ///  <summary>
        ///  Initiates a write operation to a bulk OUT endpoint.
        ///  To enable writing without blocking the main thread, uses an asynchronous delegate.
        ///  </summary>
        ///  
        ///  <param name="bytesToWrite"> The number of bytes to send </param> 
        ///  <param name="dataBuffer"> Buffer to hold the bytes to send </param> 
        ///  <param name="bytesWritten"> The number of bytes send </param>
        ///  <param name="success"> True on success </param>		

        private void RequestToSendDataViaBulkTransfer(Device dev, UInt32 bytesToWrite, Byte[] dataBuffer, ref UInt32 bytesWritten, ref Boolean success)
        {
            try
            {
                if (dataBuffer == null) throw new ArgumentNullException("dataBuffer");

                //  Define a delegate for the ReadViaBulkTransfer method of WinUsbDevice.

                SendToDeviceDelegate mySendToDeviceDelegate = _myWinUsbCommunications.SendDataViaBulkTransfer;

                //  The BeginInvoke method calls MyWinUsbDevice.SendViaBulkTransfer to attempt 
                //  to read data. The method has the same parameters as SendViaBulkTransfer,
                //  plus two additional parameters:
                //  GetBulkDataSent is the callback routine that executes when 
                //  SendViaBulkTransfer returns.
                //  MySendToDeviceDelegate is the asynchronous delegate object.

                mySendToDeviceDelegate.BeginInvoke
                    (dev._winUsbHandle,
                     dev._myDeviceInfo,
                     bytesToWrite,
                     dataBuffer,
                     ref bytesWritten,
                     ref success,
                     GetBulkDataSent,
                     mySendToDeviceDelegate);
            }
            catch (Exception ex)
            {
                DisplayException(Name, ex);
                throw;
            }
        }
        ///  <summary>
        ///  Initiates a read operation from a bulk IN endpoint.
        ///  To enable reading without blocking the main thread, uses an asynchronous delegate.
        ///  </summary>
        ///  
        ///  <param name="bytesToRead"> The number of bytes to read </param>		
        ///  <param name="dataBuffer"> Buffer to hold the bytes read </param>
        ///  <param name="bytesRead"> The number of bytes read </param>
        ///  <param name="success"> True on success </param>		

        private void RequestToReceiveDataViaBulkTransfer(Device dev, UInt32 bytesToRead, Byte[] dataBuffer, ref UInt32 bytesRead, ref Boolean success)
        {
            try
            {
                if (dataBuffer == null) throw new ArgumentNullException("dataBuffer");

                //  Define a delegate for the ReadViaBulkTransfer method of WinUsbDevice.

                ReceiveFromDeviceDelegate myReceiveFromDeviceDelegate = _myWinUsbCommunications.ReceiveDataViaBulkTransfer;

                //  The BeginInvoke method calls MyWinUsbDevice.ReceiveViaBulkTransfer to attempt 
                //  to read data. The method has the same parameters as ReceiveViaBulkTransfer,
                //  plus two additional parameters:
                //  GetBulkDataReceived is the callback routine that executes when 
                //  ReceiveViaBulkTransfer returns.
                //  MyReceiveFromDeviceDelegate is the asynchronous delegate object.

                myReceiveFromDeviceDelegate.BeginInvoke
                    (dev._winUsbHandle,
                    dev._myDeviceInfo,
                     bytesToRead,
                     ref dataBuffer,
                     ref bytesRead,
                     ref success,
                     GetBulkDataReceived,
                     myReceiveFromDeviceDelegate);
            }
            catch (Exception ex)
            {
                DisplayException(Name, ex);
                throw;
            }
        }

        ///  <summary>
        ///  Initiates a read operation from a bulk IN endpoint.
        ///  To enable reading without blocking the main thread, uses an asynchronous delegate.
        ///  </summary>
        ///  
        ///  <param name="bytesToRead"> The number of bytes to read </param>		
        ///  <param name="dataBuffer"> Buffer to hold the bytes read </param>
        ///  <param name="bytesRead"> The number of bytes read </param>
        ///  <param name="success"> True on success </param>		

        private void RequestToReceiveDataViaIsochronousTransfer(Device dev, UInt32 bytesToRead, ref Byte[] dataBuffer, ref UInt32 bytesRead, ref Boolean success)
        {
            try
            {
                if (dataBuffer == null) throw new ArgumentNullException("dataBuffer");

                //  Define a delegate for the ReadViaBulkTransfer method of WinUsbDevice.

                ReceiveFromDeviceDelegateIso myReceiveFromDeviceDelegateIso = _myWinUsbCommunications.ReceiveDataViaIsochronousTransfer;

                //  The BeginInvoke method calls MyWinUsbDevice.ReceiveViaBulkTransfer to attempt 
                //  to read data. The method has the same parameters as ReceiveViaBulkTransfer,
                //  plus two additional parameters:
                //  GetBulkDataReceived is the callback routine that executes when 
                //  ReceiveViaBulkTransfer returns.
                //  MyReceiveFromDeviceDelegate is the asynchronous delegate object.

                myReceiveFromDeviceDelegateIso.BeginInvoke
                    (dev._winUsbHandle,
                    dev._myDeviceInfo,
                     bytesToRead,
                     ref dataBuffer,
                     ref bytesRead,
                     64,
                     ref success,
                     GetBulkDataReceived,
                     myReceiveFromDeviceDelegateIso);
            }
            catch (Exception ex)
            {
                DisplayException(Name, ex);
                throw;
            }
        }

        ///  <summary>
        ///  Retrieves received data from a bulk endpoint.
        ///  This routine is called automatically when myWinUsbDevice.ReceiveViaBulkTransfer
        ///  returns. The routine calls several marshaling routines to access the main form.       
        ///  </summary>
        ///  
        ///  <param name="ar"> An object containing status information about the 
        ///  asynchronous operation.</param>
        ///  
        private void GetBulkDataReceived(IAsyncResult ar)
        {
            try
            {
                UInt32 bytesRead = 0;
                var myEncoder = new ASCIIEncoding();
                var success = false;

                Byte[] receivedDataBuffer = null;

                var thisLock = new Object();

                lock (thisLock)
                {
                    // Define a delegate using the IAsyncResult object.

                    var deleg = ((ReceiveFromDeviceDelegate)(ar.AsyncState));
                    //var deleg = ((ReceiveFromDeviceDelegateIso)(ar.AsyncState));

                    // Get the IAsyncResult object and the values of other paramaters that the
                    // BeginInvoke method passed ByRef.

                    deleg.EndInvoke
                        (ref receivedDataBuffer,
                         ref bytesRead,
                         ref success,
                         ar);
                }

                if (ar.IsCompleted)
                {
                    //Console.WriteLine("bytes read:" + bytesRead.ToString());
                    //Console.WriteLine(success);
                    //if (bytesRead >= 1)
                    //    for (Int32 i = 0; i <= bytesRead - 1; i++)
                    //    {
                    //        //Debug.WriteLine(receivedDataBuffer[i].ToString()); ;
                    //    }
                    totalByteReceived += bytesRead;
                }

                // Display the received data in the form's list box.

                if ((ar.IsCompleted && success))
                {
                    //                MyMarshalToForm(FormActions.AddItemToListBox.ToString(), "Data received via bulk transfer:");

                    //  Convert the received bytes to a String for display.
                    readSuccess = true;
                    //                String receivedtext = myEncoder.GetString(receivedDataBuffer);
                    //                MyMarshalToForm(FormActions.AddItemToListBox.ToString(), receivedtext);
                }
                else
                {
                    //                MyMarshalToForm(FormActions.AddItemToListBox.ToString(), "The attempt to read bulk data has failed.");
                }

                //            MyMarshalToForm(FormActions.ScrollToBottomOfListBox.ToString(), "");
                // Enable requesting another transfer.
                //            MyMarshalToForm(FormActions.EnableCmdSendandReceiveViaBulkTransfers.ToString(), "");
            }
            catch (Exception ex)
            {
                DisplayException(Name, ex);
                throw;
            }
        }
        ///  <summary>
        ///  Retrieves received data from a bulk endpoint.
        ///  This routine is called automatically when myWinUsbDevice.ReadViaBulkTransfer
        ///  returns. The routine calls several marshaling routines to access the main form.       
        ///  </summary>
        ///  
        ///  <param name="ar"> An object containing status information about the 
        ///  asynchronous operation.</param>
        ///  
        private void GetBulkDataSent(IAsyncResult ar)
        {
            try
            {
                UInt32 bytesSent = 0;
                var myEncoder = new ASCIIEncoding();
                var success = false;

                var thisLock = new Object();

                lock (thisLock)
                {
                    // Define a delegate using the IAsyncResult object.

                    var deleg = ((SendToDeviceDelegate)(ar.AsyncState));

                    // Get the IAsyncResult object and the values of other paramaters that the
                    // BeginInvoke method passed by reference.

                    deleg.EndInvoke(ref bytesSent, ref success, ar);
                }

                //if (ar.IsCompleted)
                //{
                //    Console.WriteLine(bytesSent);
                //    Console.WriteLine(success);
                //}

                // Display the received data in the form's list box.

                if ((ar.IsCompleted && success))
                {
                    //if (debugConsole.isRunning)
                    //    debugConsole.Invoke((MethodInvoker)delegate { debugConsole.WriteLine("Data sent via bulk transfer:" + bytesSent); });
                    // debugConsole.WriteLine("Data sent via bulk transfer:");
                }
                else
                {
                    //if (debugConsole != null)
                    // debugConsole.WriteSysMSG("The attempt to send bulk data has failed.\n");
                }

                // Enable requesting another transfer.
                //MyMarshalToForm(FormActions.EnableCmdSendandReceiveViaBulkTransfers.ToString(), "");
            }
            catch (Exception ex)
            {
                DisplayException(Name, ex);
                throw;
            }
        }
    }








    internal class SafeWinUsbHandle : SafeHandleZeroOrMinusOneIsInvalid
    {
        // Create a SafeHandle, informing the base class
        // that this SafeHandle instance "owns" the handle,
        // and therefore SafeHandle should call
        // our ReleaseHandle method when the SafeHandle
        // is no longer in use.

        internal SafeWinUsbHandle()
            : base(true)
        {
            base.SetHandle(handle);
            this.handle = IntPtr.Zero;
        }

        /// <summary>
        /// Call WinUsb_Free on releasing the handle.
        /// </summary>
        /// <returns>
        /// True on success.
        /// </returns>
        /// 
        [ReliabilityContract(Consistency.WillNotCorruptState, Cer.MayFail)]
        protected override bool ReleaseHandle()
        {
            if (!this.IsInvalid)
            {
                this.handle = IntPtr.Zero;
            }
            return WinUsbCommunications.NativeMethods.WinUsb_Free(handle);
        }

        // The IsInvalid property must be overridden. 

        public override bool IsInvalid
        {
            get
            {
                if (handle == IntPtr.Zero)
                {
                    return true;
                }
                if (handle == (IntPtr)(-1))
                {
                    return true;
                }
                return false;
            }
        }

        public IntPtr GetHandle()
        {
            if (IsInvalid)
            {
                throw new Exception("The handle is invalid.");
            }
            return handle;
        }
    }
}
