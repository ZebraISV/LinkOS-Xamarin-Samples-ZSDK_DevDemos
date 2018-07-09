﻿/***********************************************
 * CONFIDENTIAL AND PROPRIETARY 
 * 
 * The source code and other information contained herein is the confidential and exclusive property of
 * ZIH Corp. and is subject to the terms and conditions in your end user license agreement.
 * This source code, and any other information contained herein, shall not be copied, reproduced, published, 
 * displayed or distributed, in whole or in part, in any medium, by any means, for any purpose except as
 * expressly permitted under such license agreement.
 * 
 * Copyright ZIH Corp. 2018
 * 
 * ALL RIGHTS RESERVED
 ***********************************************/

/*********************************************************************************************************
File:   BaseDemoView.cs

Descr:  Base class for all the demo views. 

Date: 03/8/16 
Updated:
**********************************************************************************************************/
using LinkOS.Plugin.Abstractions;
using System;
using System.Text;
using Xamarin.Forms;

namespace Xamarin_LinkOS_Developer_Demo {

    public class BaseDemoView : StackLayout {

        public delegate void ChoosePrinterHandler();
        public static event ChoosePrinterHandler OnChoosePrinterChosen;

        private TabbedDemoPage tabbedDemoPage;
        private Label printerLbl;
        protected IDiscoveredPrinter myPrinter;

        public BaseDemoView(TabbedDemoPage tabbedDemoPage) {
            this.tabbedDemoPage = tabbedDemoPage;

            printerLbl = new Label { Text = "No Printer Selected" };

            Button selectPrinterBtn = new Button {
                Text = "Select Printer",
                HorizontalOptions = LayoutOptions.Start
            };
            selectPrinterBtn.Clicked += SelectPrinterBtn_Clicked;

            Button aboutBtn = new Button {
                Text = "About",
                HorizontalOptions = LayoutOptions.EndAndExpand
            };
            aboutBtn.Clicked += AboutBtn_Clicked;

            StackLayout topSection = new StackLayout {
                VerticalOptions = LayoutOptions.Start,
                HorizontalOptions = LayoutOptions.FillAndExpand,
                Orientation = StackOrientation.Horizontal,

                Children = { selectPrinterBtn, aboutBtn }
            };

            SelectPrinterView.OnPrinterSelected += SelectPrinterView_OnPrinterSelected;

            Children.Add(printerLbl);
            Children.Add(topSection);
        }

        private void SelectPrinterView_OnPrinterSelected(IDiscoveredPrinter printer) {
            SetPrinter(printer);
        }

        private void AboutBtn_Clicked(object sender, EventArgs e) {
            string message = "Xamarin LinkOS Developer Demo " + App.APP_Version + Environment.NewLine
                + "Using SDK " + App.API_Version + Environment.NewLine + "Copyright Zebra Technologies 2018";

            Device.BeginInvokeOnMainThread(() => {
                tabbedDemoPage.DisplayAlert("About", message, "OK");
            });
        }

        protected void SelectPrinter() {
            OnChoosePrinterChosen?.Invoke();
        }

        protected void SelectPrinterBtn_Clicked(object sender, EventArgs e) {
            tabbedDemoPage.MainPage.NavigateToSelectPrinterPage();
        }

        public void SetPrinter(IDiscoveredPrinter printer) {
            myPrinter = printer;
            printerLbl.Text = "Printer:" + printer.Address;
        }

        protected void ShowErrorAlert(string message) {
            Device.BeginInvokeOnMainThread(() => {
                tabbedDemoPage.DisplayAlert("Error", message, "OK");
            });
        }

        protected void ShowAlert(string message, string title) {
            Device.BeginInvokeOnMainThread(() => {
                tabbedDemoPage.DisplayAlert(title, message, "OK");
            });
        }

        protected bool CheckPrinter() {
            if (null == myPrinter) {
                ShowErrorAlert("Please Select a printer");
                SelectPrinter();
                return false;
            }
            return true;
        }

        protected static byte[] GetBytes(string str) {
            byte[] bytes = new byte[str.Length];
            bytes = Encoding.UTF8.GetBytes(str);
            return bytes;
        }

        protected bool CheckPrinterLanguage(IConnection connection) {
            if (!connection.IsConnected)
                connection.Open();

            //  Check the current printer language
            byte[] response = connection.SendAndWaitForResponse(GetBytes("! U1 getvar \"device.languages\"\r\n"), 500, 100);
            string language = Encoding.UTF8.GetString(response, 0, response.Length);
            if (language.Contains("line_print")) {
                ShowAlert("Switching printer to ZPL Control Language.", "Notification");
            }
            // printer is already in zpl mode
            else if (language.Contains("zpl")) {
                return true;
            }

            //  Set the printer command languege
            connection.Write(GetBytes("! U1 setvar \"device.languages\" \"zpl\"\r\n"));
            response = connection.SendAndWaitForResponse(GetBytes("! U1 getvar \"device.languages\"\r\n"), 500, 100);
            language = Encoding.UTF8.GetString(response, 0, response.Length);
            if (!language.Contains("zpl")) {
                ShowErrorAlert("Printer language not set. Not a ZPL printer.");
                return false;
            }
            return true;
        }

        protected bool PreCheckPrinterStatus(IZebraPrinter printer) {
            // Check the printer status
            IPrinterStatus status = printer.CurrentStatus;
            if (!status.IsReadyToPrint) {
                ShowErrorAlert("Unable to print. Printer is " + status.Status);
                return false;
            }
            return true;
        }

        protected bool PostPrintCheckStatus(IZebraPrinter printer) {
            // Check the status again to verify print happened successfully
            IPrinterStatus status = printer.CurrentStatus;

            // Wait while the printer is printing
            while ((status.NumberOfFormatsInReceiveBuffer > 0) && (status.IsReadyToPrint)) {
                status = printer.CurrentStatus;
            }

            // verify the print didn't have errors like running out of paper
            if (!status.IsReadyToPrint) {
                ShowErrorAlert("Error durring print. Printer is " + status.Status);
                return false;
            }
            return true;
        }
    }
}
