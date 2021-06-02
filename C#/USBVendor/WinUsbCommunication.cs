using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.Linq;
using System.Text;
using System.Threading.Tasks;


namespace USBVendorNS
{
    //public class WinUsbCommunication
    //{
    //    void _WinUsbCommunication()
    //    {

    //    }
    //    readonly WinUsbCommunications _myWinUsbCommunications = new WinUsbCommunications();
    //    private string Name = "WinUsbCommunication";
    //    private bool readSuccess;
    //    private uint totalByteReceived;

    //    ///  <summary>
    //    ///  Define a class of delegates with the same parameters as 
    //    ///  WinUsbDevice.SendViaBulkTransfer.
    //    ///  Used for asynchronous writes to the device.
    //    ///  </summary>		

    //    private delegate void SendToDeviceDelegate
    //        (WinUsbCommunications.SafeWinUsbHandle winUsbHandle,
    //        WinUsbCommunications.DeviceInfo myDevInfo,
    //        UInt32 bufferLength,
    //        Byte[] buffer,
    //        ref UInt32 lengthTransferred,
    //        ref Boolean success);

    //    ///  <summary>
    //    ///  Define a class of delegates with the same parameters as 
    //    ///  WinUsbDevice.ReadViaBulkTransfer.
    //    ///  Used for asynchronous reads from the device.
    //    ///  </summary>

    //    private delegate void ReceiveFromDeviceDelegate(WinUsbCommunications.SafeWinUsbHandle winUsbHandle, WinUsbCommunications.DeviceInfo myDeviceInfo, UInt32 bytesToRead, ref Byte[] dataBuffer, ref UInt32 bytesRead, ref Boolean success);


    //    ///  <summary>
    //    ///  Initiates a write operation to a bulk OUT endpoint.
    //    ///  To enable writing without blocking the main thread, uses an asynchronous delegate.
    //    ///  </summary>
    //    ///  
    //    ///  <param name="bytesToWrite"> The number of bytes to send </param> 
    //    ///  <param name="dataBuffer"> Buffer to hold the bytes to send </param> 
    //    ///  <param name="bytesWritten"> The number of bytes send </param>
    //    ///  <param name="success"> True on success </param>		

    //    public void RequestToSendDataViaBulkTransfer(Device dev, UInt32 bytesToWrite, Byte[] dataBuffer, ref UInt32 bytesWritten, ref Boolean success)
    //    {
    //        try
    //        {
    //            if (dataBuffer == null) throw new ArgumentNullException("dataBuffer");

    //            //  Define a delegate for the ReadViaBulkTransfer method of WinUsbDevice.

    //            SendToDeviceDelegate mySendToDeviceDelegate = _myWinUsbCommunications.SendDataViaBulkTransfer;

    //            //  The BeginInvoke method calls MyWinUsbDevice.SendViaBulkTransfer to attempt 
    //            //  to read data. The method has the same parameters as SendViaBulkTransfer,
    //            //  plus two additional parameters:
    //            //  GetBulkDataSent is the callback routine that executes when 
    //            //  SendViaBulkTransfer returns.
    //            //  MySendToDeviceDelegate is the asynchronous delegate object.

    //            mySendToDeviceDelegate.BeginInvoke
    //                (dev._winUsbHandle,
    //                 dev._myDeviceInfo,
    //                 bytesToWrite,
    //                 dataBuffer,
    //                 ref bytesWritten,
    //                 ref success,
    //                 GetBulkDataSent,
    //                 mySendToDeviceDelegate);
    //        }
    //        catch (Exception ex)
    //        {
    //            DisplayException(Name, ex);
    //            throw;
    //        }
    //    }
    //    ///  <summary>
    //    ///  Initiates a read operation from a bulk IN endpoint.
    //    ///  To enable reading without blocking the main thread, uses an asynchronous delegate.
    //    ///  </summary>
    //    ///  
    //    ///  <param name="bytesToRead"> The number of bytes to read </param>		
    //    ///  <param name="dataBuffer"> Buffer to hold the bytes read </param>
    //    ///  <param name="bytesRead"> The number of bytes read </param>
    //    ///  <param name="success"> True on success </param>		

    //    public void RequestToReceiveDataViaBulkTransfer(Device dev, UInt32 bytesToRead, Byte[] dataBuffer, ref UInt32 bytesRead, ref Boolean success)
    //    {
    //        try
    //        {
    //            if (dataBuffer == null) throw new ArgumentNullException("dataBuffer");

    //            //  Define a delegate for the ReadViaBulkTransfer method of WinUsbDevice.

    //            ReceiveFromDeviceDelegate myReceiveFromDeviceDelegate = _myWinUsbCommunications.ReceiveDataViaBulkTransfer;

    //            //  The BeginInvoke method calls MyWinUsbDevice.ReceiveViaBulkTransfer to attempt 
    //            //  to read data. The method has the same parameters as ReceiveViaBulkTransfer,
    //            //  plus two additional parameters:
    //            //  GetBulkDataReceived is the callback routine that executes when 
    //            //  ReceiveViaBulkTransfer returns.
    //            //  MyReceiveFromDeviceDelegate is the asynchronous delegate object.

    //            myReceiveFromDeviceDelegate.BeginInvoke
    //                (dev._winUsbHandle,
    //                dev._myDeviceInfo,
    //                 bytesToRead,
    //                 ref dataBuffer,
    //                 ref bytesRead,
    //                 ref success,
    //                 GetBulkDataReceived,
    //                 myReceiveFromDeviceDelegate);
    //        }
    //        catch (Exception ex)
    //        {
    //            DisplayException(Name, ex);
    //            throw;
    //        }
    //    }

    //    ///  <summary>
    //    ///  Retrieves received data from a bulk endpoint.
    //    ///  This routine is called automatically when myWinUsbDevice.ReceiveViaBulkTransfer
    //    ///  returns. The routine calls several marshaling routines to access the main form.       
    //    ///  </summary>
    //    ///  
    //    ///  <param name="ar"> An object containing status information about the 
    //    ///  asynchronous operation.</param>
    //    ///  
    //    private void GetBulkDataReceived(IAsyncResult ar)
    //    {
    //        try
    //        {
    //            UInt32 bytesRead = 0;
    //            var myEncoder = new ASCIIEncoding();
    //            var success = false;

    //            Byte[] receivedDataBuffer = null;

    //            var thisLock = new Object();

    //            lock (thisLock)
    //            {
    //                // Define a delegate using the IAsyncResult object.

    //                var deleg = ((ReceiveFromDeviceDelegate)(ar.AsyncState));

    //                // Get the IAsyncResult object and the values of other paramaters that the
    //                // BeginInvoke method passed ByRef.

    //                deleg.EndInvoke
    //                    (ref receivedDataBuffer,
    //                     ref bytesRead,
    //                     ref success,
    //                     ar);
    //            }

    //            if (ar.IsCompleted)
    //            {
    //                Debug.WriteLine("bytes read:" + bytesRead.ToString());
    //                Debug.WriteLine(success);
    //                totalByteReceived += bytesRead;
    //            }

    //            // Display the received data in the form's list box.

    //            if ((ar.IsCompleted && success))
    //            {
    //                //                MyMarshalToForm(FormActions.AddItemToListBox.ToString(), "Data received via bulk transfer:");

    //                //  Convert the received bytes to a String for display.
    //                readSuccess = true;
    //                //                String receivedtext = myEncoder.GetString(receivedDataBuffer);

    //                //                MyMarshalToForm(FormActions.AddItemToListBox.ToString(), receivedtext);
    //            }
    //            else
    //            {
    //                //                MyMarshalToForm(FormActions.AddItemToListBox.ToString(), "The attempt to read bulk data has failed.");
    //            }

    //            //            MyMarshalToForm(FormActions.ScrollToBottomOfListBox.ToString(), "");

    //            // Enable requesting another transfer.

    //            //            MyMarshalToForm(FormActions.EnableCmdSendandReceiveViaBulkTransfers.ToString(), "");
    //        }
    //        catch (Exception ex)
    //        {
    //            DisplayException(Name, ex);
    //            throw;
    //        }
    //    }
    //    ///  <summary>
    //    ///  Retrieves received data from a bulk endpoint.
    //    ///  This routine is called automatically when myWinUsbDevice.ReadViaBulkTransfer
    //    ///  returns. The routine calls several marshaling routines to access the main form.       
    //    ///  </summary>
    //    ///  
    //    ///  <param name="ar"> An object containing status information about the 
    //    ///  asynchronous operation.</param>
    //    ///  
    //    private void GetBulkDataSent(IAsyncResult ar)
    //    {
    //        try
    //        {
    //            UInt32 bytesSent = 0;
    //            var myEncoder = new ASCIIEncoding();
    //            Byte[] sentDataBuffer;
    //            String receivedtext = "";
    //            var success = false;

    //            var thisLock = new Object();

    //            lock (thisLock)
    //            {
    //                // Define a delegate using the IAsyncResult object.

    //                var deleg = ((SendToDeviceDelegate)(ar.AsyncState));

    //                // Get the IAsyncResult object and the values of other paramaters that the
    //                // BeginInvoke method passed by reference.

    //                deleg.EndInvoke(ref bytesSent, ref success, ar);
    //            }

    //            if (ar.IsCompleted)
    //            {
    //                Debug.WriteLine(bytesSent);
    //                Debug.WriteLine(success);
    //            }

    //            // Display the received data in the form's list box.

    //            //if ((ar.IsCompleted && success))
    //            //{
    //            //    MyMarshalToForm(FormActions.AddItemToListBox.ToString(), "Data sent via bulk transfer:");
    //            //}
    //            //else
    //            //{
    //            //    MyMarshalToForm(FormActions.AddItemToListBox.ToString(), "The attempt to send bulk data has failed.");
    //            //}

    //            //MyMarshalToForm(FormActions.ScrollToBottomOfListBox.ToString(), "");
    //        }
    //        catch (Exception ex)
    //        {
    //            DisplayException(Name, ex);
    //            throw;
    //        }
    //    }
    //    internal static void DisplayException(String name, Exception e)
    //    {
    //        try
    //        {
    //            //  Create an error message.

    //            String message = "Exception: " + e.Message + Environment.NewLine + "Module: " + name + Environment.NewLine + "Method: " + e.TargetSite.Name;

    //            const string caption = "Unexpected Exception";

    //            System.Windows.Forms.MessageBox.Show(message, caption, System.Windows.Forms.MessageBoxButtons.OK);
    //            Debug.Write(message);
    //        }
    //        catch (Exception ex)
    //        {
    //            DisplayException(name, ex);
    //            throw;
    //        }
    //    }


    //}
}
