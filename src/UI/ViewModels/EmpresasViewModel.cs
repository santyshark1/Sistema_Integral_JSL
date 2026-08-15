using System;
using System.Collections.Generic;
using System.Collections.ObjectModel;
using System.Linq;
using System.Threading.Tasks;
using System.Windows.Input;
using JSL_SentinelPro.src.Core;
using JSL_SentinelPro.src.Models;

namespace JSL_SentinelPro.src.UI.ViewModels
{
    /// <summary>
    /// ViewModel de empresas asociadas.
    /// </summary>
    public class EmpresasViewModel : BaseViewModel
    {
        private readonly DatabaseService _database;

        private ObservableCollection<CompanyPartner> _partners = new ObservableCollection<CompanyPartner>();
        private ObservableCollection<CompanyPartner> _filteredPartners = new ObservableCollection<CompanyPartner>();
        private string _selectedCity = "Todas";
        private string _selectedSpecialty = "Todas";
        private string _searchText = string.Empty;
        private List<string> _cities = new List<string>();
        private List<string> _specialties = new List<string>();
        private ObservableCollection<PartnerAppointment> _appointments = new ObservableCollection<PartnerAppointment>();
        private bool _showAppointments;

        public ObservableCollection<CompanyPartner> Partners { get => _partners; set => SetProperty(ref _partners, value); }
        public ObservableCollection<CompanyPartner> FilteredPartners { get => _filteredPartners; set => SetProperty(ref _filteredPartners, value); }
        public string SelectedCity { get => _selectedCity; set { if (SetProperty(ref _selectedCity, value)) Filter(); } }
        public string SelectedSpecialty { get => _selectedSpecialty; set { if (SetProperty(ref _selectedSpecialty, value)) Filter(); } }
        public string SearchText { get => _searchText; set { if (SetProperty(ref _searchText, value)) Filter(); } }
        public List<string> Cities { get => _cities; set => SetProperty(ref _cities, value); }
        public List<string> Specialties { get => _specialties; set => SetProperty(ref _specialties, value); }
        public ObservableCollection<PartnerAppointment> Appointments { get => _appointments; set => SetProperty(ref _appointments, value); }
        public bool ShowAppointments { get => _showAppointments; set => SetProperty(ref _showAppointments, value); }

        public ICommand ScheduleAppointmentCommand { get; }
        public ICommand ToggleAppointmentsCommand { get; }
        public ICommand ContactCommand { get; }
        public ICommand RequestDiagnosisCommand { get; }
        public ICommand ClearFiltersCommand { get; }

        public EmpresasViewModel(DatabaseService database)
        {
            _database = database;

            ScheduleAppointmentCommand = new RelayCommand<CompanyPartner>(p => ScheduleAppointment(p));
            ToggleAppointmentsCommand = new RelayCommand(_ => ShowAppointments = !ShowAppointments);
            ContactCommand = new RelayCommand<CompanyPartner>(p => Contact(p));
            RequestDiagnosisCommand = new RelayCommand<CompanyPartner>(p => RequestDiagnosis(p));
            ClearFiltersCommand = new RelayCommand(_ => ClearFilters());

            LoadPartnersAsync();
            LoadAppointmentsAsync();
        }

        private async void LoadPartnersAsync()
        {
            var list = await _database.GetCompanyPartnersAsync();
            Partners = new ObservableCollection<CompanyPartner>(list);
            FilteredPartners = new ObservableCollection<CompanyPartner>(list);
            Cities = new List<string> { "Todas" };
            Cities.AddRange(list.Select(p => p.City).Distinct().OrderBy(c => c));
            Specialties = new List<string> { "Todas" };
            Specialties.AddRange(list.Select(p => p.Specialty).Distinct().OrderBy(s => s));
        }

        private void Filter()
        {
            var query = Partners.AsEnumerable();
            if (SelectedCity != "Todas")
                query = query.Where(p => p.City == SelectedCity);
            if (SelectedSpecialty != "Todas")
                query = query.Where(p => p.Specialty == SelectedSpecialty);
            if (!string.IsNullOrWhiteSpace(SearchText))
                query = query.Where(p => p.Name.Contains(SearchText, StringComparison.OrdinalIgnoreCase) ||
                                          p.City.Contains(SearchText, StringComparison.OrdinalIgnoreCase) ||
                                          p.Specialty.Contains(SearchText, StringComparison.OrdinalIgnoreCase) ||
                                          p.Address.Contains(SearchText, StringComparison.OrdinalIgnoreCase));
            FilteredPartners = new ObservableCollection<CompanyPartner>(query.OrderByDescending(p => p.Rating));
        }

        private void ClearFilters()
        {
            SelectedCity = "Todas";
            SelectedSpecialty = "Todas";
            SearchText = string.Empty;
        }

        private async void LoadAppointmentsAsync()
        {
            var appointments = await _database.GetPartnerAppointmentsAsync();
            Appointments = new ObservableCollection<PartnerAppointment>(appointments);
        }

        private async void ScheduleAppointment(CompanyPartner? partner)
        {
            if (partner == null) return;
            var appointment = new PartnerAppointment
            {
                RequestedAt = DateTime.Now,
                CompanyName = partner.Name,
                City = partner.City,
                Specialty = partner.Specialty,
                Contact = $"{partner.Phone} / {partner.Email}",
                Status = "Solicitada - pendiente de confirmacion"
            };
            appointment.Id = await _database.SavePartnerAppointmentAsync(appointment);
            Appointments.Insert(0, appointment);
            ShowAppointments = true;
            System.Windows.MessageBox.Show($"Cita solicitada con {partner.Name}. Pronto recibira confirmacion.", "Cita Agendada", System.Windows.MessageBoxButton.OK, System.Windows.MessageBoxImage.Information);
        }

        private void Contact(CompanyPartner? partner)
        {
            if (partner == null) return;
            System.Windows.MessageBox.Show($"Telefono: {partner.Phone}\nEmail: {partner.Email}", "Contacto", System.Windows.MessageBoxButton.OK, System.Windows.MessageBoxImage.Information);
        }

        private void RequestDiagnosis(CompanyPartner? partner)
        {
            if (partner == null) return;
            System.Windows.MessageBox.Show($"Solicitud de diagnostico enviada a {partner.Name}.", "Diagnostico", System.Windows.MessageBoxButton.OK, System.Windows.MessageBoxImage.Information);
        }
    }
}
