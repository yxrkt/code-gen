using System;
using System.ComponentModel;
using System.IO;
using System.Runtime.CompilerServices;
using System.Windows;
using System.Windows.Threading;

namespace CodeGen
{
    public class BaseVM : INotifyPropertyChanged
    {
        public event PropertyChangedEventHandler PropertyChanged;

        protected bool SetProperty<T>(ref T field, T value, [CallerMemberName] string propertyName = "")
        {
            if (!Equals(field, value))
            {
                field = value;
                PropertyChanged?.Invoke(this, new PropertyChangedEventArgs(propertyName));
                return true;
            }

            return false;
        }
    }

    public class DocumentVM : BaseVM
    {
        private string json = string.Empty;
        private string cpp = string.Empty;
        private string error = string.Empty;

        private DispatcherTimer delayTimer;

        public DocumentVM()
        {
            delayTimer = new DispatcherTimer(
                interval: TimeSpan.FromSeconds(0.5),
                priority: DispatcherPriority.Normal,
                callback: OnJsonUpdated,
                dispatcher: Application.Current.Dispatcher);

            Json = File.ReadAllText("types.json");
        }

        public string Json
        {
            get { return json; }
            set
            {
                if (SetProperty(ref json, value))
                {
                    delayTimer.Stop();
                    delayTimer.Start();
                }
            }
        }

        public string Cpp
        {
            get { return cpp; }
            set { SetProperty(ref cpp, value); }
        }

        public string Error
        {
            get { return error; }
            set { SetProperty(ref error, value); }
        }

        private void OnJsonUpdated(object sender, EventArgs e)
        {
            try
            {
                Cpp = CodeGenerator.GenerateCode(Json);
                Error = string.Empty;
            }
            catch (Exception ex)
            {
                Error = ex.Message;
            }
            finally
            {
                delayTimer.Stop();
            }
        }
    }
}
