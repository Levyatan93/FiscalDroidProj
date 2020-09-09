using Android.App;
using Android.OS;
using Android.Support.V7.App;
using Android.Runtime;
using Android.Widget;
using System.Collections.Generic;
using Android.Bluetooth;
using System;
using System.Linq;

using Java.Util;
using Android.Views;
using AlertDialog = Android.App.AlertDialog;
using System.Threading;

namespace FiscalDroid
{
    [Activity(Label = "@string/app_name", Theme = "@style/AppTheme", MainLauncher = true)]
    public class MainActivity : AppCompatActivity
    {
       
        Spinner list;
        Button Con;
        Button BtnRapX;
        ParcelUuid DevUID;
        BluetoothSocket btSocket;
        protected override void OnCreate(Bundle savedInstanceState)
        {
            base.OnCreate(savedInstanceState);
            Xamarin.Essentials.Platform.Init(this, savedInstanceState);
            // Set our view from the "main" layout resource
            SetContentView(Resource.Layout.activity_main);
            BluetoothAdapter adapterbt = BluetoothAdapter.DefaultAdapter;
            if (adapterbt == null)
                throw new Exception("No Bluetooth adapter found.");

            if (!adapterbt.IsEnabled)
                throw new Exception("Bluetooth adapter is not enabled.");
            List<string> paired = new List<string>();

            foreach (BluetoothDevice device in adapterbt.BondedDevices)
            {
                paired.Add(device.Name);
            }
            var adapter = new ArrayAdapter<string>(this, Android.Resource.Layout.SimpleSpinnerItem, paired);
            
             list = (Spinner)FindViewById(Resource.Id.spinner1);
            Con = (Button)FindViewById(Resource.Id.Connect);
            Con.Click += Connect;
            BtnRapX = (Button)FindViewById(Resource.Id.RapX);
            BtnRapX.Click += SendX;
            list.Adapter = adapter;
            
        }
        public override void OnRequestPermissionsResult(int requestCode, string[] permissions, [GeneratedEnum] Android.Content.PM.Permission[] grantResults)
        {
            Xamarin.Essentials.Platform.OnRequestPermissionsResult(requestCode, permissions, grantResults);

            base.OnRequestPermissionsResult(requestCode, permissions, grantResults);
        }

        public async void Connect(object sender, EventArgs args)
        {
            BluetoothAdapter adapterbt = BluetoothAdapter.DefaultAdapter;
            BluetoothDevice device = (from bd in adapterbt.BondedDevices
                                      where bd.Name == list.SelectedItem.ToString()
                                      select bd).FirstOrDefault();
            
            Android.App.AlertDialog.Builder dialog = new AlertDialog.Builder(this);
            AlertDialog alert = dialog.Create();
            alert.SetTitle("Title");
            ParcelUuid[] uuids = device.GetUuids();
            if ((uuids != null) && (uuids.Length > 0))
            {
                foreach (var uuid in uuids)
                {
                    try
                    {

                        btSocket = device.CreateRfcommSocketToServiceRecord(uuid.Uuid);
                        if(!btSocket.IsConnected)
                        await btSocket.ConnectAsync();
                        if (btSocket.IsConnected)
                        {
                            Mutex mut = new Mutex();
                            byte[] Login =
                            {0x01,0x30,0x30,0x32,0x3A,0x20,0x30,0x30,0x34,0x3A,0x05,0x30,0x31,0x3B,0x3F,0x03};
                            byte[] RapX = { 0x01, 0x30, 0x30, 0x32, 0x3C, 0x2E, 0x30, 0x30, 0x34, 0x35, 0x58, 0x09, 0x05, 0x30, 0x32, 0x32, 0x3B, 0x03 };
                            DevUID = uuid;

                            Console.WriteLine("conected to:" + uuid.Uuid.ToString());
                            Talk2BTsocket(btSocket, Login, mut);
                            
                            break;
                        }
                       
                    }
                    catch (Exception ex)
                    {

                        Toast.MakeText(this, ex.Message, ToastLength.Short);
                    }
                   
                    //var _socket = device.CreateRfcommSocketToServiceRecord(UUID.FromString(device.GetUuids().ToString()));
                    //await _socket.ConnectAsync();
                }
            }
        }
        public async void SendX(object sender, EventArgs args)

        {
            BluetoothAdapter adapterbt = BluetoothAdapter.DefaultAdapter;
            BluetoothDevice device = (from bd in adapterbt.BondedDevices
                                      where bd.Name == list.SelectedItem.ToString()
                                      select bd).FirstOrDefault();
            btSocket = device.CreateRfcommSocketToServiceRecord(DevUID.Uuid);
            Console.WriteLine(DevUID.Uuid.ToString());
            var outStream = btSocket.OutputStream;
            var inStream = btSocket.InputStream;
            Mutex mut = new Mutex();
            byte[] Login =
            {
                0x01,0x30,0x30,0x32,0x3A,0x20,0x30,0x30,0x34,0x3A,0x05,0x30,0x31,0x3B,0x3F,0x03
            };
            byte[] RapX = { 0x01, 0x30, 0x30, 0x32, 0x3C, 0x2E, 0x30, 0x30, 0x34, 0x35, 0x58, 0x09, 0x05, 0x30, 0x32, 0x32, 0x3B, 0x03 };
            if (!btSocket.IsConnected)
            {
                
                try
                {
                    await btSocket.ConnectAsync();
                }
                catch (Exception ex)
                {
                    Console.WriteLine(ex.Message);
                }
            }
           
            if (btSocket.IsConnected)
            {
                try
                {
                    
                    outStream.Write(RapX, 0, RapX.Length);
                }
                catch(Exception ex)
                {
                    Console.WriteLine(ex.Message);
                }
                //Talk2BTsocket(btSocket, RapX, mut);


            }

            
        }

        byte[] Talk2BTsocket(BluetoothSocket socket, byte[] cmd, Mutex _mx, int timeOut = 150)
        {
            var buf = new byte[0x20];

            _mx.WaitOne();
            try
            {
                using (var ost = socket.OutputStream)
                {
                    var _ost = (ost as OutputStreamInvoker).BaseOutputStream;
                    _ost.Write(cmd, 0, cmd.Length);
                }

                // needed because when skipped, it can cause no or invalid data on input stream
                Thread.Sleep(timeOut);

                using (var ist = socket.InputStream)
                {
                    var _ist = (ist as InputStreamInvoker).BaseInputStream;
                    var aa = 0;
                    if ((aa = _ist.Available()) > 0)
                    {
                        var nn = _ist.Read(buf, 0, aa);
                        System.Array.Resize(ref buf, nn);
                    }
                }
            }
            catch (System.Exception ex)
            {
               Console.Write("Exception!!!" +ex.Message);
            }
            finally
            {
                _mx.ReleaseMutex();     // must be called here !!!
            }

            return buf;
        }

    }
}