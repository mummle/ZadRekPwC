using Soneta.Business;
using Soneta.Business.UI;
using Soneta.Kadry;
using Soneta.Kalend;
using Soneta.Tools;
using Soneta.Types;

using System;

using ZadRekPwC.UI.Workers;

[assembly: Worker(typeof(AddAbsenceWorker), typeof(PracHistoria))]

namespace ZadRekPwC.UI.Workers
{
    public class AddAbsenceWorker
    {
        [Context]
        public AddAbsenceWorkerParams Pars { get; set; }

        [Action("Zadania Rekrutacyjne PwC/Dodaj nieobecność", Mode = ActionMode.SingleSession, Target = ActionTarget.ToolbarWithText, Icon = ActionIcon.Wizard)]
        public string RunWithoutConfirmation()
        {
            AddAbsence();
            return "Operacja została wykonana";
        }

        private void AddAbsence()
        {
            var pracownik = (Pracownik)this.Pars.Context[typeof(Pracownik),false];

            FromTo okres = new FromTo(Pars.Data,Pars.Data);

            if (Pars.Nieobecnosc == null) return;

            if (pracownik.NieobecnośćWgDaty(Pars.Data) != null)
                throw new Exception("{0} ma już nieobecność w dniu {1}. Operacja została przerwana.".TranslateFormat((object)pracownik, (object)this.Pars.Data));

            using (ITransaction t = this.Pars.Context.Session.Logout(true))
            {
                var km = this.Pars.Context.Session.GetKalend().Nieobecnosci;
                var nieobecnoscPracownika = new NieobecnośćPracownika(pracownik);
                km.AddRow(nieobecnoscPracownika);
                nieobecnoscPracownika.Definicja = Pars.Nieobecnosc;
                nieobecnoscPracownika.Okres = okres;

                t.Commit();
            }
        }
    }
    public class AddAbsenceWorkerParams : ContextBase
    {
        private Date data;
        private DefinicjaNieobecnosci nieobecnosc;
        public AddAbsenceWorkerParams(Context context) : base(context)
        {
            this.data = ((ActualDate)context[typeof(ActualDate)]).Actual;
        }


        [Required]
        [Priority(1)]
        [Caption("Data")]
        public Date Data
        {
            get => this.data; set
            {
                this.data = value;
                this.OnChanged(EventArgs.Empty);
            }
        }

        [Priority(2)]
        [Caption("Nieobecność")]
        public DefinicjaNieobecnosci Nieobecnosc
        {
            get => this.nieobecnosc;
            set
            {
                this.nieobecnosc = value;
            }
        }

        public LookupInfo GetListNieobecnosc()
        {
            var fc = new FieldCondition.Equal("Blokada", false);
            var st = this.Session.GetKalend().DefNieobecnosci.WgNazwy[fc];
            return new LookupInfo(st);
        }
    }
}