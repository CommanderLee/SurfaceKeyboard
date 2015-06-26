using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Windows;
using System.Windows.Controls;
using System.Windows.Data;
using System.Windows.Documents;
using System.Windows.Input;
using System.Windows.Media;
using System.Windows.Media.Imaging;
using System.Windows.Shapes;

namespace SurfaceKeyboard_Mobile
{
    /// <summary>
    /// WindowUserId.xaml 的交互逻辑
    /// </summary>
    public partial class WindowUserId : Window
    {
        private String userId;

        public WindowUserId()
        {
            InitializeComponent();
            userId = "";
            textBoxUserId.Focus();
            Keyboard.Focus(textBoxUserId);
        }

        private void button1_Click(object sender, RoutedEventArgs e)
        {
            userId = textBoxUserId.Text;
            this.Close();
        }

        /* The main window call this function to get the string */
        public String getUserId()
        {
            return userId;
        }

        private void Window_Closing(object sender, System.ComponentModel.CancelEventArgs e)
        {
            if (userId == "")
            {
                MessageBox.Show("Error: Empty User ID.");
                e.Cancel = true;
            }
        }
    }
}
