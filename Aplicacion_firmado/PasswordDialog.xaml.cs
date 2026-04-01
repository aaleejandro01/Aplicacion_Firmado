using System;
using System.Collections.Generic;
using System.Text;
using System.Windows;
using System.Windows.Controls;
using System.Windows.Data;
using System.Windows.Documents;
using System.Windows.Input;
using System.Windows.Media;
using System.Windows.Media.Imaging;
using System.Windows.Shapes;

namespace Aplicacion_firmado
{
    public partial class PasswordDialog : Window
    {
        //Variable que guarda la contraseña para poder leerla en MainWindow.xaml
        public string PasswordIntroducida { get; private set; }
        public PasswordDialog()
        {
            InitializeComponent();
            TxtPassword.Focus(); //Pone el cursor directamente en la caja al abrirse
        }

        private void BtnAceptar_Click(object sender, RoutedEventArgs e)
        {
            PasswordIntroducida = TxtPassword.Password;
            this.DialogResult = true; //Comunica a la pantalla ppal que todo ha ido bien
            this.Close();
        }

        private void BtnCancelar_Click(Object sender, RoutedEventArgs e)
        {
            this.DialogResult = false;
            this.Close();
        }
    }
}
