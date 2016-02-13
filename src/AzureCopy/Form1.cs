using Microsoft.WindowsAzure.Storage;
using Microsoft.WindowsAzure.Storage.Auth;
using Microsoft.WindowsAzure.Storage.Blob;
using System;
using System.Collections.Generic;
using System.ComponentModel;
using System.Data;
using System.Drawing;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using System.Windows.Forms;
using System.Diagnostics;
using System.Threading;

namespace AzureCopy
{
    public enum AccountType
    {
        Source, Destination
    }

    public partial class Form1 : Form
    {
        public Form1()
        {
            InitializeComponent();
        }
        Stopwatch sw;
        private void button1_Click(object sender, EventArgs e)
        {
            printList("Initiating Connections...");
            var sourceAccountClient = GetAccount(AccountType.Source).CreateCloudBlobClient();
            var destiAccountClient = GetAccount(AccountType.Destination).CreateCloudBlobClient();
            sw = new Stopwatch();
            clearList();
            sw.Start();
            Task.Run(() =>
            {
                while (true)
                {
                    label5.Invoke(new Action(() => {
                        label5.Text = sw.Elapsed.ToString("mm\\:ss");
                    }));
                    Thread.Sleep(1000);
                }
            });
            Task.Run(() =>
            {
                foreach (CloudBlobContainer cont in sourceAccountClient.ListContainers())
                {
                    var targetContainer = destiAccountClient.GetContainerReference(cont.Name);
                    printList(string.Format("Creating Container {0}...", targetContainer.Name));
                    targetContainer.CreateIfNotExists();

                    foreach (IListBlobItem srcBlob in cont.ListBlobs(useFlatBlobListing: true))
                    {
                        Uri thisBlobUri = srcBlob.Uri;
                        var serverBlob = sourceAccountClient.GetBlobReferenceFromServer(thisBlobUri);


                        CloudBlockBlob sourceBlob = cont.GetBlockBlobReference(serverBlob.Name);
                        var sharedAccessURI = GetShareAccessUri(serverBlob.Name, 360, cont);

                        CloudBlockBlob targetBlob = targetContainer.GetBlockBlobReference(serverBlob.Name);

                        printList(string.Format(" - Copying the blob {0}...", targetBlob.Name));
                        targetBlob.StartCopyFromBlob(new Uri(sharedAccessURI));
                    }
                }
            }).ContinueWith((x) => {
                printList("Done!");
                sw.Stop();
            });

            //foreach(var container in sourceAccountClient.ListContainers())
            //{
            //    var sourceContainer = sourceAccountClient.GetContainerReference(container.Name);
            //    var targetContainer = destiAccountClient.GetContainerReference(container.Name);
            //    targetContainer.CreateIfNotExists();

            //    printList(string.Format("Creating Container {0}...", container.Name));

            //    foreach(var blob in container.ListBlobs())
            //    {
            //        ICloudBlob targetBlob=targetContainer.GetBlobReferenceFromServer(blob)
            //    }
            //}
        }

        private string GetShareAccessUri(string blobname,
                                 int validityPeriodInMinutes,
                                 CloudBlobContainer container)
        {
            var toDateTime = DateTime.Now.AddMinutes(validityPeriodInMinutes);

            var policy = new SharedAccessBlobPolicy
            {
                Permissions = SharedAccessBlobPermissions.Read,
                SharedAccessStartTime = null,
                SharedAccessExpiryTime = new DateTimeOffset(toDateTime)
            };

            var blob = container.GetBlockBlobReference(blobname);
            var sas = blob.GetSharedAccessSignature(policy);
            return blob.Uri.AbsoluteUri + sas;
        }


        public CloudStorageAccount GetAccount(AccountType type)
        {
            StorageCredentials re;
            if (type == AccountType.Source)
            {
                re = new StorageCredentials(textBox1.Text, textBox2.Text);
                return  new CloudStorageAccount(re, useHttps: true);
            }
            else
            {
                re = new StorageCredentials(textBox4.Text, textBox3.Text);
                return new CloudStorageAccount(re, useHttps: true);
            }
        }
        public void msg(object text)
        {
            MessageBox.Show(text.ToString());
        }
        public void printList(object text)
        {
            listBox1.Invoke(new Action(() =>
            {
                listBox1.Items.Add(text);
            }));
        }
        public void clearList()
        {
            listBox1.Items.Clear();
        }
    }
}
