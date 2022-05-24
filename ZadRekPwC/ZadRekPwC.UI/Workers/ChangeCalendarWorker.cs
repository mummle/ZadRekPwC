using Soneta.Business;
using Soneta.Business.UI;
using Soneta.Kadry;
using Soneta.Kalend;
using Soneta.Tools;
using Soneta.Types;

using System;
using System.Collections.Generic;
using System.Threading;

using ZadRekPwC.UI.Workers;

[assembly: Worker(typeof(ChangeCalendarWorker), typeof(Pracownicy))]

namespace ZadRekPwC.UI.Workers
{
    public class ChangeCalendarWorker
    {
        [Context]
        public Session Session { get; set; }
        [Context]
        public ChangeCalendarParams Pars { get; set; }

        [Action("Zadania Rekrutacyjne PwC/Zmień kalendarz", Mode = ActionMode.SingleSession | ActionMode.Progress, Target = ActionTarget.ToolbarWithText, Icon = ActionIcon.Wizard)]

        public string RunWithoutConfirmation()
        {
            ChangeCalendar();
            return "Operacja została wykonana";
        }

        private void ChangeCalendar()
        {
            using (ITransaction t = this.Pars.Context.Session.Logout(true))
            {
                Log log1 = new Log("Zaktualizuj kalendarz".Translate(), true);
                Log log2 = new Log();
                int length = this.Pars.Pracownicy.Length;
                int num = 0;
                foreach (Pracownik p in this.Pars.Pracownicy)
                {
                    log2.WriteLine((object)p);
                    PracHistoria pracHistoria1 = p[this.Pars.Data];
                    if (pracHistoria1.Aktualnosc.From == this.Pars.Data)
                        log1.WriteLine("{0} ma już aktualizację od {1}. Aktualizacja nie została wykonana.".TranslateFormat((object)p, (object)this.Pars.Data));
                    else if (this.Pars.TylkoOstatni && pracHistoria1.Aktualnosc.To < Date.MaxValue)
                    {
                        log1.WriteLine("{0} ma aktualizację późniejszą niż wybrana data {1}. Aktualizacja nie została wykonana.".TranslateFormat((object)p, (object)this.Pars.Data));
                    }
                    else
                    {
                        PracHistoria pracHistoria2 = p.Historia.Update(this.Pars.Data);
                        p.Module.PracHistorie.AddRow((Row)pracHistoria2);
                        ((IUpdateReason)pracHistoria2).UpdateReason = this.Pars.PowodAktualizacji;
                        pracHistoria2.Etat.Kalendarz = this.Pars.Kalendarz;
                    }
                    log2.WriteLine((object)new Percent((long)++num, (long)length));
                }
                t.Commit();
            }
        }
    }

    public class ChangeCalendarParams : ContextBase
    {
        private Date data;
        private Kalendarz kalendarz;
        private Pracownik[] pracownicy;
        private string powodAktualizacji;
        private bool tylkoOstatni;

        public ChangeCalendarParams(Context context) : base(context)
        {
            this.data = ((ActualDate)context[typeof(ActualDate)]).Actual;
            var st = this.Session.GetKalend().Kalendarze.WgNazwy[TypKalendarza.Kalendarz];
            this.kalendarz = st.GetFirst() as Kalendarz;
            this.pracownicy = ((Pracownik[])context[typeof(Pracownik[])]);
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
        [Caption("Kalendarz")]
        public Kalendarz Kalendarz
        {
            get => this.kalendarz;
            set
            {
                this.kalendarz = value;
                this.OnChanged(EventArgs.Empty);
            }
        }

        public LookupInfo GetListKalendarz()
        {
            var st = this.Session.GetKalend().Kalendarze.WgNazwy[TypKalendarza.Kalendarz];
            return new LookupInfo(st) { ComboBox = true };
        }
        [Priority(3)]
        [Caption("Pracownicy")]
        public Pracownik[] Pracownicy
        {
            get => this.pracownicy;
            set
            {
                this.pracownicy = value;
            }
        }

        public LookupInfo GetListPracownicy()
        {
            var km = this.Session.GetKadry().Pracownicy;
            List <Pracownik> ls = new List<Pracownik>();
            foreach (Pracownik p in km)
            {
                PracHistoria ph = p[this.Data];
                if (ph.Etat.OkresZatrudnieniaEtat.To >= this.Data)
                    ls.Add(ph.Pracownik);
            }
            return new LookupInfo(ls);

        }
        [Priority(4)]
        [Caption("Tylko ostatni zapis")]
        public bool TylkoOstatni
        {
            get => this.tylkoOstatni;
            set
            {
                this.tylkoOstatni = value;
                this.OnChanged(EventArgs.Empty);
            }
        }

        [Priority(5)]
        [MaxLength(80)]
        [Caption("Powód aktualizacji")]
        [Dictionary("ETA.PowódAkt")]
        public string PowodAktualizacji
        {
            get => this.powodAktualizacji;
            set
            {
                this.powodAktualizacji = value;
                this.OnChanged(EventArgs.Empty);
            }
        }
    }
}
