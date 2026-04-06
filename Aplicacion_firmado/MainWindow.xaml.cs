using System.IO;
using System.Text;
using System.Windows;
using System.Windows.Controls;
using System.Windows.Data;
using System.Windows.Documents;
using System.Windows.Input;
using System.Windows.Media;
using System.Windows.Media.Imaging;
using System.Windows.Navigation;
using System.Windows.Shapes;

namespace Aplicacion_firmado
{
    public partial class MainWindow : Window
    {
        // VARIABLE GLOBAL: Aquí guardaremos la ruta exacta del archivo que vamos a firmar.
        private string rutaArchivoActual = "";
        // VARIABLE GLOBAL: Aquí guardaremos la ruta de la carpeta actual a la que apunta el explorador de archivos.
        private string rutaCarpetaActual = "";
        // VARIABLE GLOBAL: Hash de la contraseña maestra.
        private const string HashPasswordMaestra = "$2a$12$PaMCh7ENGox3X2PNKLMDqOOfttkT/hFZZzOWflxJHjw7lHOIfLQg2";
        public MainWindow()
        {
            InitializeComponent();

            // Para generar las claves privadas
            string carpetaClaves = System.IO.Path.Combine(AppDomain.CurrentDomain.BaseDirectory, "Claves");
            System.IO.Directory.CreateDirectory(carpetaClaves);
        }

        // EVENTO: Cuando el usuario hace clic en "Abrir Carpeta..."
        private void BtnAbrirCarpeta_Click(object sender, RoutedEventArgs e)
        {
            // Usamos el diálogo clásico para elegir carpeta
            var dialog = new System.Windows.Forms.FolderBrowserDialog();
            dialog.Description = "Selecciona la carpeta raíz para el proyecto de firmado";

            if (dialog.ShowDialog() == System.Windows.Forms.DialogResult.OK)
            {
                string rutaSeleccionada = dialog.SelectedPath;
                rutaCarpetaActual = rutaSeleccionada;
                CargarArbolDeArchivos(rutaSeleccionada);
            }
        }

        // Carga la carpeta en el TreeView
        private void CargarArbolDeArchivos(string rutaRaiz)
        {
            TvExplorador.Items.Clear(); // Limpiamos lo que hubiera antes

            DirectoryInfo dirInfo = new DirectoryInfo(rutaRaiz);
            TreeViewItem nodoRaiz = CrearNodoRecursivo(dirInfo);

            nodoRaiz.IsExpanded = true; // Que la carpeta principal salga abierta
            TvExplorador.Items.Add(nodoRaiz);
        }

        // Lee carpetas y archivos y los convierte en elementos visuales
        private TreeViewItem CrearNodoRecursivo(DirectoryInfo directorio)
        {
            TreeViewItem nodo = new TreeViewItem
            {
                Header = directorio.Name,
                Tag = directorio.FullName // Guardamos la ruta oculta en la propiedad Tag
            };

            try
            {
                // Primero añadimos las subcarpetas
                foreach (DirectoryInfo subDir in directorio.GetDirectories())
                {
                    nodo.Items.Add(CrearNodoRecursivo(subDir));
                }

                // Luego añadimos los archivos
                foreach (FileInfo archivo in directorio.GetFiles())
                {
                    TreeViewItem nodoArchivo = new TreeViewItem
                    {
                        Header = archivo.Name,
                        Tag = archivo.FullName // Guardamos la ruta exacta del archivo
                    };
                    nodo.Items.Add(nodoArchivo);
                }
            }
            catch (UnauthorizedAccessException)
            {
                // Ignoramos carpetas del sistema a las que Windows no nos deja entrar
            }

            return nodo;
        }

        // EVENTO: Cuando el usuario hace clic en un archivo del explorador
        private void TvExplorador_SelectedItemChanged(object sender, RoutedPropertyChangedEventArgs<object> e)
        {
            if (TvExplorador.SelectedItem is TreeViewItem nodoSeleccionado)
            {
                if (nodoSeleccionado.Tag is string ruta)
                {
                    if (System.IO.File.Exists(ruta))
                    {
                        // 1. Guardamos la ruta completa en nuestra variable para usarla luego al firmar
                        rutaArchivoActual = ruta;

                        // 2. Cambiamos el texto del panel derecho
                        TxtNombreArchivo.Text = System.IO.Path.GetFileName(ruta);
                        TxtNombreArchivoVerificar.Text = System.IO.Path.GetFileName(ruta);

                        // 3. Lo ponemos en color negro para que destaque (ya no es el texto gris por defecto)
                        TxtNombreArchivo.Foreground = System.Windows.Media.Brushes.Black;

                        //4. Muestra la ruta completa al pasar el ratón
                        TxtNombreArchivo.ToolTip = ruta;

                        try
                        {
                            var archivo = new ArchivoSSP(ruta);
                            if (archivo.TieneFirma)
                            {
                                // Buscamos y seleccionamos el algoritmo en el ComboBox
                                foreach (ComboBoxItem item in CmbAlgoritmoVerificar.Items)
                                {
                                    if (item.Content.ToString() == archivo.AlgoritmoDetectado)
                                    {
                                        CmbAlgoritmoVerificar.SelectedItem = item;
                                        break;
                                    }
                                }

                                // Buscamos y seleccionamos el Hash
                                foreach (ComboBoxItem item in CmbHashVerificar.Items)
                                {
                                    if (item.Content.ToString() == archivo.HashDetectado)
                                    {
                                        CmbHashVerificar.SelectedItem = item;
                                        break;
                                    }
                                }

                                TxtMensajeEstadoVerificar.Text = "Configuración de firma detectada automáticamente.";
                                TxtMensajeEstadoVerificar.Foreground = System.Windows.Media.Brushes.Blue;
                            }
                        }
                        catch { /* Ignorar si falla la lectura rápida */ }
                    }
                }
            }
        }

        // EVENTO: Cuando el usuario cambia el algoritmo de firmado
        private void CmbAlgoritmo_SelectionChanged(object sender, SelectionChangedEventArgs e)
        {
            // Verificamos que los componentes ya estén cargados en pantalla
            if (CmbAlgoritmo == null || CmbHash == null) return;

            if (CmbAlgoritmo.SelectedItem is ComboBoxItem itemSeleccionado)
            {
                string algoritmoElegido = itemSeleccionado.Content.ToString();

                if (algoritmoElegido == "EdDSA Ed25519")
                {
                    // Si es Ed25519, quitamos la selección y bloqueamos el desplegable
                    CmbHash.SelectedIndex = -1;
                    CmbHash.IsEnabled = false;
                }
                else
                {
                    // Si es cualquier ECDSA, lo volvemos a habilitar
                    CmbHash.IsEnabled = true;
                }
            }
        }

        // EVENTO VISUAL: Ocultar el Hash en Verificar si se exige Ed25519
        private void CmbAlgoritmoVerificar_SelectionChanged(object sender, SelectionChangedEventArgs e)
        {
            // Evitamos errores si la ventana aún se está dibujando
            if (CmbAlgoritmoVerificar == null || CmbHashVerificar == null) return;

            if (CmbAlgoritmoVerificar.SelectedItem is ComboBoxItem itemSeleccionado)
            {
                string algoritmoElegido = itemSeleccionado.Content.ToString();

                if (algoritmoElegido == "EdDSA Ed25519")
                {
                    // Si es Ed25519, quitamos la selección y bloqueamos el desplegable de Hash
                    CmbHashVerificar.SelectedIndex = -1;
                    CmbHashVerificar.IsEnabled = false;
                }
                else
                {
                    // Si es cualquier ECDSA, lo volvemos a habilitar
                    CmbHashVerificar.IsEnabled = true;
                }
            }
        }

        // EVENTO: Cuando el usuario cambia el procesador
        private void CmbProcesador_SelectionChanged(object sender, SelectionChangedEventArgs e)
        {
            // Evitamos errores si la ventana aún se está dibujando
            if (CmbProcesador == null || TxtRecomendacion == null) return;

            if (CmbProcesador.SelectedItem is ComboBoxItem itemSeleccionado)
            {
                string procesadorElegido = itemSeleccionado.Content.ToString();

                switch (procesadorElegido)
                {
                    case "Renesas Rx72N":
                        TxtRecomendacion.Text = "La mejor configuración para este procesador es combinar el algoritmo de hash SHA-256 con el algoritmo de firmado ECDSA-256. " +
                            "\nSi no se puediera usar SHA-256, la segunda mejor opción es blake2 pero este cálculo se realiza por software. " +
                            "\nEn cuanto a los algoritmos de firmado, ECDSA-192 está algo antiguo y ECDSA-384 esta diseñado para procesadores de 64 bits, su uso obligaria a usar software lo cual haría el procesao mas lento";
                        break;

                    case "Renesas Rx660":
                        TxtRecomendacion.Text = "La mejor configuración para este procesador es combinar el algoritmo de hash SHA-256 con el algoritmo de firmado ECDSA-256. " +
                            "\nSi no se puediera usar SHA-256, la segunda mejor opción es blake2 pero este cálculo se realiza por software. " +
                            "\nEn cuanto a los algoritmos de firmado, ECDSA-192 está algo antiguo y ECDSA-384 esta diseñado para procesadores de 64 bits, su uso obligaria a usar software lo cual haría el procesao mas lento";
                        break;

                    case "Renesas Rx140":
                        TxtRecomendacion.Text = "La mejor configuración para este procesador es combinar el algoritmo de hash SHA-256 con el algoritmo de firmado ECDSA-256. " +
                            "\nSi no se puediera usar SHA-256, la segunda mejor opción es blake2 pero este cálculo se realiza por software. " +
                            "\nEn cuanto a los algoritmos de firmado, ECDSA-192 está algo antiguo y ECDSA-384 esta diseñado para procesadores de 64 bits, su uso obligaria a usar software lo cual haría el procesao mas lento";
                        break;

                    case "TI TMS 320F28075":
                        TxtRecomendacion.Text = "Dado que no dispone de hardware dedicado y todos los cálculos se tendrían que optimizar por software, el mejor algoritmo de firmado es EdDSA Ed25519";
                        break;

                    case "TI TMS320F28335":
                        TxtRecomendacion.Text = "Dado que no dispone de hardware dedicado y todos los cálculos se tendrían que optimizar por software, el mejor algoritmo de firmado es EdDSA Ed25519";
                        break;

                    // POR DEFECTO
                    default:
                        TxtRecomendacion.Text = "Selecciona un procesador de la lista para ver las recomendaciones de firma específicas de su arquitectura.";
                        break;
                }
            }
        }

        // EVENTO: Cambiar entre los 4 modos de la App
        private void CmbModoApp_SelectionChanged(object sender, SelectionChangedEventArgs e)
        {
            if (PanelFirma == null || PanelVerificacion == null || PanelClaves == null || PanelCertificados == null) return;

            if (CmbModoApp.SelectedItem is ComboBoxItem itemSeleccionado)
            {
                string modoElegido = itemSeleccionado.Content.ToString();

                // 1. Apagamos todos los paneles
                PanelFirma.Visibility = Visibility.Collapsed;
                PanelVerificacion.Visibility = Visibility.Collapsed;
                PanelClaves.Visibility = Visibility.Collapsed;
                PanelCertificados.Visibility = Visibility.Collapsed;

                // 2. Encendemos solo el que toca según el texto de tu XAML
                switch (modoElegido)
                {
                    case "Modo: Firmar":
                        PanelFirma.Visibility = Visibility.Visible;
                        break;
                    case "Modo: Verificar":
                        PanelVerificacion.Visibility = Visibility.Visible;
                        break;
                    case "Generar Claves":
                        PanelClaves.Visibility = Visibility.Visible;
                        break;
                    case "Generar Certificados":
                        PanelCertificados.Visibility = Visibility.Visible;
                        break;
                }
            }
        }

        // EVENTO: Cuando el usuario pulsa el botón FIRMAR
        private void BtnFirmar_Click(object sender, RoutedEventArgs e)
        {
            // 1.1 Comprobación: ¿Hay un archivo seleccionado?
            if (string.IsNullOrEmpty(rutaArchivoActual) || !System.IO.File.Exists(rutaArchivoActual))
            {
                TxtMensajeEstado.Text = "Por favor, selecciona un archivo en el explorador primero.";
                TxtMensajeEstado.Foreground = System.Windows.Media.Brushes.Red;
                return;
            }

            // 1.2 Comprobación: ¿Ha elegido el Algoritmo de firmado?
            if (CmbAlgoritmo.SelectedItem == null)
            {
                TxtMensajeEstado.Text = "Por favor, selecciona el Algoritmo de firmado.";
                TxtMensajeEstado.Foreground = System.Windows.Media.Brushes.Red;
                return;
            }

            string algoritmoElegido = ((ComboBoxItem)CmbAlgoritmo.SelectedItem).Content.ToString();
            string hashElegido = "No aplica (Ed25519)"; // Valor por defecto si es Ed25519

            // 1.3 Comprobación del Hash (SOLO si NO es Ed25519)
            if (algoritmoElegido != "EdDSA Ed25519")
            {
                if (CmbHash.SelectedItem == null)
                {
                    TxtMensajeEstado.Text = "Por favor, selecciona un Algoritmo de HASH.";
                    TxtMensajeEstado.Foreground = System.Windows.Media.Brushes.Red;
                    return;
                }
                hashElegido = ((ComboBoxItem)CmbHash.SelectedItem).Content.ToString();
            }

            // 2. Pedir contraseña para firmar
            PasswordDialog dialogo = new PasswordDialog();
            dialogo.Owner = this;
            if (dialogo.ShowDialog() != true) return; //Si cancela, salimos

            // 3. Verificar contra el hash
            if (!BCrypt.Net.BCrypt.Verify(dialogo.PasswordIntroducida, HashPasswordMaestra))
            {
                System.Windows.MessageBox.Show("Contraseña incorrecta. Acceso denegado.", "Seguridad", MessageBoxButton.OK, MessageBoxImage.Stop);
                return;
            }

            // 3. Buscar la clave dinámicamente en la carpeta "Claves"
            // AppDomain.CurrentDomain.BaseDirectory es la carpeta donde se está ejecutando el .exe
            string carpetaClaves = System.IO.Path.Combine(AppDomain.CurrentDomain.BaseDirectory, "Claves");
            string rutaClavePem = System.IO.Path.Combine(carpetaClaves, $"{algoritmoElegido}.pem");

            // Si el archivo de la clave no existe, avisamos y cortamos
            if (!System.IO.File.Exists(rutaClavePem))
            {
                TxtMensajeEstado.Text = $"No se encuentra la clave: {algoritmoElegido}.pem en la carpeta Claves.";
                TxtMensajeEstado.Foreground = System.Windows.Media.Brushes.Red;
                return;
            }

            try
            {
                TxtMensajeEstado.Text = $"Analizando archivo y firmando con {algoritmoElegido}...";
                TxtMensajeEstado.Foreground = System.Windows.Media.Brushes.Orange;

                // Usamos clase ArchivoSSP para leer el archivo.
                // Esto separa automáticamente el contenido de una posible firma antigua.
                var archivoSsp = new ArchivoSSP(rutaArchivoActual);

                // Control de seguridad: Si ya tiene firma, avisamos y no permitimos refirmar
                if (archivoSsp.TieneFirma)
                {
                    TxtMensajeEstado.Text = "El archivo ya contiene una firma 'SG '. No se puede volver a firmar.";
                    TxtMensajeEstado.Foreground = System.Windows.Media.Brushes.Red;
                    return;
                }

                // Leemos la clave directamente desde el archivo físico
                string miClavePrivadaPem = System.IO.File.ReadAllText(rutaClavePem);

                // Creamos el objeto de firmar
                using var signer = new FirmwareSigner(miClavePrivadaPem, algoritmoElegido, hashElegido, dialogo.PasswordIntroducida);

                // Importante: Usamos archivoSsp.ContenidoOriginal.
                // Si el archivo es virgen, son todos los bytes. Si tuviera firma antigua (que hemos controlado arriba),
                // serían los bytes sin esa firma.
                byte[] datosOriginales = archivoSsp.ContenidoOriginal;
                byte[] firmaGenerada = signer.FirmarDatos(datosOriginales);

                // Convertimos los bytes incomprensibles de la firma a texto legible (Base64)
                string firmaEnTexto = Convert.ToBase64String(firmaGenerada);

                // Añadimos la firma directamente en la última línea del archivo original.
                ArchivoSSP.IncrustarFirma(rutaArchivoActual, firmaEnTexto, algoritmoElegido, hashElegido);

                // 5. ¡Éxito!
                TxtMensajeEstado.Text = $"✅ Archivo firmado con éxito ({algoritmoElegido} / {hashElegido})";
                TxtMensajeEstado.Foreground = System.Windows.Media.Brushes.Green;

                //Recargamos el explorador de archivos
                if (!string.IsNullOrEmpty(rutaCarpetaActual))
                {
                    CargarArbolDeArchivos(rutaCarpetaActual);
                }

                //Guardamos los logs
                string carpetaLogs = System.IO.Path.Combine(AppDomain.CurrentDomain.BaseDirectory, "Logs");
                //Nos aseguramos de que si no hay carpeta logs, la crea
                System.IO.Directory.CreateDirectory(carpetaLogs);
                string nombreArchivoLog = $"historial_{DateTime.Now:yyyy-MM-dd}.log";
                string rutaArchivoLog = System.IO.Path.Combine(carpetaLogs, nombreArchivoLog);
                string lineaLog = $"[{DateTime.Now:HH:mm:ss}] [FIRMA] Archivo: {System.IO.Path.GetFileName(rutaArchivoActual)} | Algoritmos: {algoritmoElegido} / {hashElegido} | Estado: EXITO\n";
                System.IO.File.AppendAllText(rutaArchivoLog, lineaLog);
            }
            catch (Exception ex)
            {
                TxtMensajeEstado.Text = $"❌ Error al firmar: {ex.Message}";
                TxtMensajeEstado.Foreground = System.Windows.Media.Brushes.Red;
            }
        }

        // EVENTO: Cuando el usuario pulsa VERIFICAR FIRMA
        private void BtnVerificar_Click(object sender, RoutedEventArgs e)
        {
            // Comprobamos que se ha seleccionado un archivo
            if (string.IsNullOrEmpty(rutaArchivoActual) || !System.IO.File.Exists(rutaArchivoActual))
            {
                TxtMensajeEstadoVerificar.Text = "❌ Selecciona un archivo original a la izquierda.";
                TxtMensajeEstadoVerificar.Foreground = System.Windows.Media.Brushes.Red;
                return;
            }
            // Comprobamos que haya elegido un algoritmo de firmado
            if (CmbAlgoritmoVerificar.SelectedItem == null)
            {
                TxtMensajeEstadoVerificar.Text = "❌ Define el algoritmo exigido.";
                TxtMensajeEstadoVerificar.Foreground = System.Windows.Media.Brushes.Red;
                return;
            }

            string algoritmoExigido = ((ComboBoxItem)CmbAlgoritmoVerificar.SelectedItem).Content.ToString();
            string hashExigido = "No aplica (Ed25519)";

            if (algoritmoExigido != "EdDSA Ed25519")
            {
                if (CmbHashVerificar.SelectedItem == null)
                {
                    TxtMensajeEstadoVerificar.Text = "❌ Define el Hash exigido.";
                    TxtMensajeEstadoVerificar.Foreground = System.Windows.Media.Brushes.Red;
                    return;
                }
                hashExigido = ((ComboBoxItem)CmbHashVerificar.SelectedItem).Content.ToString();
            }

            try
            {
                TxtMensajeEstadoVerificar.Text = "⏳ Leyendo archivo y comprobando firma incrustada...";
                TxtMensajeEstadoVerificar.Foreground = System.Windows.Media.Brushes.Orange;

                // Usamos clase ArchivoSSP para "despiezar" el archivo seleccionado.
                var archivoSsp = new ArchivoSSP(rutaArchivoActual);

                // Comprobamos si el archivo tiene el formato correcto (marcador "SG ")
                if (!archivoSsp.TieneFirma)
                {
                    TxtMensajeEstadoVerificar.Text = "❌ El archivo no contiene ninguna firma incrustada (falta 'SG ').";
                    TxtMensajeEstadoVerificar.Foreground = System.Windows.Media.Brushes.Red;
                    return;
                }

                // Extraemos la firma y el contenido limpio directamente de la clase.
                string firmaBase64 = archivoSsp.FirmaExtraida;
                byte[] firmaBytes = Convert.FromBase64String(firmaBase64);
                byte[] datosOriginales = archivoSsp.ContenidoOriginal; // Este es el mensaje SIN la última línea.

                string carpetaClaves = System.IO.Path.Combine(AppDomain.CurrentDomain.BaseDirectory, "Claves");
                string rutaClavePem = System.IO.Path.Combine(carpetaClaves, $"{algoritmoExigido}.pem");

                if (!System.IO.File.Exists(rutaClavePem))
                {
                    TxtMensajeEstadoVerificar.Text = $"❌ No se encuentra la clave para {algoritmoExigido}.";
                    TxtMensajeEstadoVerificar.Foreground = System.Windows.Media.Brushes.Red;
                    return;
                }

                string clavePem = System.IO.File.ReadAllText(rutaClavePem);

                using var verificador = new FirmwareVerifier(clavePem, algoritmoExigido, hashExigido);
                bool esValida = verificador.VerificarFirma(datosOriginales, firmaBytes);

                if (esValida)
                {
                    TxtMensajeEstadoVerificar.Text = "✅ FIRMA VÁLIDA. El archivo es auténtico.";
                    TxtMensajeEstadoVerificar.Foreground = System.Windows.Media.Brushes.Green;
                }
                else
                {
                    TxtMensajeEstadoVerificar.Text = "🚨 FIRMA INVÁLIDA. Posible modificación.";
                    TxtMensajeEstadoVerificar.Foreground = System.Windows.Media.Brushes.Red;
                }
                //Guardamos la informacion de la verificacion en los logs
                string carpetaLogs = System.IO.Path.Combine(AppDomain.CurrentDomain.BaseDirectory, "Logs");
                System.IO.Directory.CreateDirectory(carpetaLogs); // Nos aseguramos de que la carpeta exista

                string nombreArchivoLog = $"historial_{DateTime.Now:yyyy-MM-dd}.log";
                string rutaArchivoLog = System.IO.Path.Combine(carpetaLogs, nombreArchivoLog);

                string estadoLog = esValida ? "ÉXITO (Válida)" : "ALERTA (Inválida o Modificada)";
                string lineaLog = $"[{DateTime.Now:HH:mm:ss}] [VERIFICACIÓN] Archivo: {System.IO.Path.GetFileName(rutaArchivoActual)} | Algoritmo Exigido: {algoritmoExigido} / {hashExigido} | Resultado: {estadoLog}\n";

                System.IO.File.AppendAllText(rutaArchivoLog, lineaLog);
            }
            catch (Exception ex)
            {
                TxtMensajeEstadoVerificar.Text = $"❌ Error crítico: {ex.Message}";
                TxtMensajeEstadoVerificar.Foreground = System.Windows.Media.Brushes.Red;
            }
        }

        // EVENTO: Generacion de claves
        private void BtnGenerarNuevasClaves_Click(object sender, System.Windows.RoutedEventArgs e)
        {
            //Instanciamos y abrimos la ventana de la contraseña
            PasswordDialog dialogo = new PasswordDialog();
            dialogo.Owner = this;

            //Se detiene la ejecución aquí hasta que el usuario cierre la ventana
            bool? resultado = dialogo.ShowDialog();

            if (resultado == true)
            {
                //Compara la contraseña en plano introducida por el usuario, con el hash guardado
                bool passwordCorrecta = BCrypt.Net.BCrypt.Verify(dialogo.PasswordIntroducida, HashPasswordMaestra);
                
                if (passwordCorrecta)
                {
                    try
                    {
                        TxtEstadoGeneracion.Foreground = System.Windows.Media.Brushes.Orange;
                        TxtEstadoGeneracion.Text = "Generando entropía y calculando claves...";

                        string algoritmoSeleccionado = ((System.Windows.Controls.ComboBoxItem)CmbAlgoritmoGenerar.SelectedItem).Content.ToString();
                        string carpetaClaves = System.IO.Path.Combine(System.AppDomain.CurrentDomain.BaseDirectory, "Claves");

                        // La ruta EXACTA que usan tus botones de Firmar y Verificar
                        string rutaPrivadaPrincipal = System.IO.Path.Combine(carpetaClaves, $"{algoritmoSeleccionado}.pem");

                        // 1. SISTEMA DE SEGURIDAD: Si ya existe una clave vieja, no la borramos, le cambiamos el nombre a "backup"
                        if (System.IO.File.Exists(rutaPrivadaPrincipal))
                        {
                            string fechaBackup = DateTime.Now.ToString("yyyyMMdd_HHmmss");
                            string rutaBackupPrivada = System.IO.Path.Combine(carpetaClaves, $"{algoritmoSeleccionado}_backup_{fechaBackup}_privada.pem");
                            System.IO.File.Move(rutaPrivadaPrincipal, rutaBackupPrivada);

                            // Si existe la pública vieja, la backupeamos también
                            string rutaPublicaPrincipal = rutaPrivadaPrincipal.Replace(".pem", "_publica.pem");
                            if (System.IO.File.Exists(rutaPublicaPrincipal))
                            {
                                string rutaBackupPublica = System.IO.Path.Combine(carpetaClaves, $"{algoritmoSeleccionado}_backup_{fechaBackup}_publica.pem");
                                System.IO.File.Move(rutaPublicaPrincipal, rutaBackupPublica);
                            }
                        }

                        // 2. Generamos las nuevas claves con el nombre PRINCIPAL
                        GeneradorClaves.CrearClavePrivada(algoritmoSeleccionado, rutaPrivadaPrincipal, dialogo.PasswordIntroducida);

                        TxtEstadoGeneracion.Foreground = System.Windows.Media.Brushes.Green;
                        TxtEstadoGeneracion.Text = $"Nuevo par {algoritmoSeleccionado} generado. Las claves anteriores se han guardado como backup.";
                    }
                    catch (System.Exception ex) 
                    {
                        TxtEstadoGeneracion.Foreground = System.Windows.Media.Brushes.Red;
                        TxtEstadoGeneracion.Text = $"Error al generar: {ex.Message}";
                    }
                }
                //En caso de que la contraseña sea incorrecta
                else
                { 
                    System.Windows.MessageBox.Show("Contraseña de administrador incorrecta. Operación cancelada.", "Acceso Denegado", System.Windows.MessageBoxButton.OK, System.Windows.MessageBoxImage.Error);
                    TxtEstadoGeneracion.Text = "";
                }
            }
            //El usuario pulsó CANCELAR en la ventana
            else
            {
                TxtEstadoGeneracion.Text = "";
            }
        }
    }
}