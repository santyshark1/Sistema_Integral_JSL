using System;
using System.Collections.Generic;
using System.Collections.ObjectModel;
using System.Diagnostics;
using System.IO;
using System.Linq;
using System.Threading.Tasks;
using System.Windows.Input;
using JSL_SentinelPro.src.Core;

namespace JSL_SentinelPro.src.UI.ViewModels
{
    public class AyudaViewModel : BaseViewModel
    {
        private ObservableCollection<FaqItem> _faqs = new ObservableCollection<FaqItem>();
        private string _searchText = string.Empty;
        private string _supportEmail = "jhernandezp14@ucentral.edu.co; ecardenasg3@ucentral.edu.co; lestupinang@ucentral.edu.co";
        private string _supportPhone = "+57 1 800 123 456";

        public ObservableCollection<FaqItem> Faqs { get => _faqs; set => SetProperty(ref _faqs, value); }
        public string SearchText { get => _searchText; set { if (SetProperty(ref _searchText, value)) FilterFaqs(); } }
        public string SupportEmail { get => _supportEmail; set => SetProperty(ref _supportEmail, value); }
        public string SupportPhone { get => _supportPhone; set => SetProperty(ref _supportPhone, value); }

        public ICommand OpenDocsCommand { get; }
        public ICommand ContactSupportCommand { get; }

        public AyudaViewModel()
        {
            OpenDocsCommand = new RelayCommand(async _ => await OpenDocsAsync());
            ContactSupportCommand = new RelayCommand(_ => ContactSupport());
            LoadFaqs();
        }

        private void LoadFaqs()
        {
            Faqs = new ObservableCollection<FaqItem>
            {
                new FaqItem { Question = "Como ejecuto un escaneo de hardware?", Answer = "Vaya a Diagnostico y haga clic en Iniciar Escaneo. Luego puede usar Reconoce tu PC." },
                new FaqItem { Question = "Que hago si detecto una amenaza?", Answer = "En Ciberseguridad, seleccione la amenaza y elija Cuarentena o Eliminar." },
                new FaqItem { Question = "Como limpio archivos temporales?", Answer = "En Mantenimiento, seleccione los elementos y presione Limpiar Ahora." },
                new FaqItem { Question = "Necesito permisos de administrador?", Answer = "Algunas funciones del sistema pueden requerir permisos elevados." },
                new FaqItem { Question = "Como genero reportes?", Answer = "Use los botones Generar PDF o Exportar CSV en cada modulo cuando haya informacion disponible." }
            };
        }

        private void FilterFaqs()
        {
            LoadFaqs();
            if (!string.IsNullOrWhiteSpace(SearchText))
            {
                Faqs = new ObservableCollection<FaqItem>(
                    Faqs.Where(f => f.Question.Contains(SearchText, StringComparison.OrdinalIgnoreCase) ||
                                    f.Answer.Contains(SearchText, StringComparison.OrdinalIgnoreCase)));
            }
        }

        private async Task OpenDocsAsync()
        {
            var lines = new List<string>
            {
                "JSL SentinelPro - Documentacion de uso",
                $"Fecha: {DateTime.Now:yyyy-MM-dd HH:mm}",
                "",
                "Inicio",
                "Muestra CPU, RAM, disco, temperatura, historial de escaneos y permite generar un PDF de escaneo. Si la temperatura sale alta, siga las recomendaciones y contacte empresas asociadas.",
                "",
                "Diagnostico",
                "Use Iniciar Escaneo para leer hardware. Luego Reconoce tu PC genera un informe con procesador, RAM, discos, tarjeta grafica, red, vida util estimada y mejoras recomendadas.",
                "",
                "Ciberseguridad",
                "Iniciar Escaneo revisa amenazas con Windows Defender y rutas criticas. Si aparece una amenaza, seleccione la fila y use Cuarentena o Eliminar. Ignorar solo debe usarse si conoce el archivo.",
                "",
                "Mantenimiento",
                "Seleccione elementos de limpieza y pulse Limpiar Ahora para liberar espacio. En Optimizacion marque solo los ajustes que desea aplicar y pulse Optimizar Ahora. El PDF explica lo realizado.",
                "",
                "Reportes",
                "Resume salud global, amenazas, mantenimientos, rendimiento historico y datos tecnicos. Exportar CSV crea una tabla para auditoria y Generar PDF crea un resumen ejecutivo.",
                "",
                "Usuarios",
                "Los administradores pueden crear, actualizar y eliminar usuarios. Los usuarios estandar solo ven su propia informacion.",
                "",
                "Empresas",
                "Permite filtrar por ciudad, especialidad o busqueda para contactar especialistas en mantenimiento, seguridad, refrigeracion o mejoras SSD/RAM.",
                "",
                "Configuracion",
                "Permite programar escaneos, mantenimiento automatico, notificaciones y cambiar la contrasena del usuario actual.",
                "",
                "Consejo general",
                "Ejecute escaneos periodicos, mantenga espacio libre, no instale software sospechoso y haga mantenimiento fisico si hay temperaturas altas."
            };

            var path = Path.Combine(Environment.GetFolderPath(Environment.SpecialFolder.DesktopDirectory), $"JSL_Documentacion_{DateTime.Now:yyyyMMdd_HHmmss}.pdf");
            await PdfDocumentWriter.WriteAsync(path, lines, "Documentacion JSL SentinelPro");
            try { Process.Start(new ProcessStartInfo(path) { UseShellExecute = true }); } catch { }
        }

        private void ContactSupport()
        {
            try
            {
                Process.Start(new ProcessStartInfo("mailto:jhernandezp14@ucentral.edu.co;ecardenasg3@ucentral.edu.co;lestupinang@ucentral.edu.co") { UseShellExecute = true });
            }
            catch { }
        }
    }

    public class FaqItem
    {
        public string Question { get; set; } = string.Empty;
        public string Answer { get; set; } = string.Empty;
        public bool IsExpanded { get; set; }
    }
}
