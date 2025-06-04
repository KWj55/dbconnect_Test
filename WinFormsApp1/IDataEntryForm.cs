using System.Collections.Generic;

namespace WinFormsApp1
{
    public interface IDataEntryForm
    {
        Dictionary<string, object> FormData { get; }
        bool ValidateInputs();
        void InitializeInputFields();
        void CollectFormData();
    }
} 