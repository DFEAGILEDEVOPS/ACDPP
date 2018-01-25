using System.Collections.Generic;

namespace uk.gov {
    public class SmartFormsModelBase {
        public virtual bool HasErrors() {
            return false;
        }

        public virtual IEnumerable<Error> GetErrors() {
            return new List<Error>();
        }

        public class Error
        {
            public string Id {get; private set;}
            public string Message {get; private set;}

            public Error(string id, string message) {
                Id = id;
                Message = message;
            }
        }
    }
}