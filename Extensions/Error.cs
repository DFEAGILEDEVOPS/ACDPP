using System;
using System.Collections.Generic;

namespace Extensions
{
    public class Error
    {
        public enum Types
        {
            Unknown = 0,
            Successful = 1,
            Exception = 2
        }

        protected string _Details;

        protected int _ID = -1;

        public Error(string Details)
        {
            _ID = 2;
            _Details = Details;
        }

        public int ID
        {
            get { return _ID; }
            set { _ID = value; }
        }

        public string Details
        {
            get { return _Details; }
            set { _Details = value; }
        }
    }

    public class HandledValidationException : Exception
    {
        public new readonly string Message;

        public HandledValidationException(string message)
            : base(message)
        {
            Message = message.ReplaceI("chilkat", "mkryptor");
        }

        public HandledValidationException(List<string> errors)
        {
            Message = null;
            if (errors != null)
                for (var i = 0; i < errors.Count; i++)
                {
                    errors[i] = errors[i].ReplaceI("chilkat", "mkryptor");
                    if (Message != null) Message += "\n";
                    Message += errors[i];
                }
            Errors = errors;
        }

        public HandledValidationException(string message, List<string> errors)
            : base(message)
        {
            Message = message.ReplaceI("chilkat", "mkryptor");
            if (errors != null)
                for (var i = 0; i < errors.Count; i++)
                {
                    errors[i] = errors[i].ReplaceI("chilkat", "mkryptor");
                    if (Message != null) Message += "\n";
                    Message += errors[i];
                }
            Errors = errors;
        }

        public List<string> Errors { get; set; }
    }

    public class HandledException : Exception
    {
        public enum Types
        {
            Unknown = 0,
            PermanentFailure = 1,
            TransientFailure = 2,
            Success = 3
        }

        public HandledException(string message, Types type = Types.TransientFailure)
        {
            Type = type;
            _Message = message.ReplaceI("chilkat", "mkryptor");
        }

        public HandledException(List<string> errors, Types type = Types.TransientFailure)
        {
            Type = type;
            _Message = null;
            if (errors != null)
            {
                for (var i = 0; i < errors.Count; i++)
                {
                    errors[i] = errors[i].ReplaceI("chilkat", "mkryptor");
                    if (Message != null) _Message += "\n";
                    _Message += errors[i];
                }
            }
            Errors = errors;
        }

        public HandledException(string message, List<string> errors, Types type = Types.TransientFailure)
            : base(message)
        {
            Type = type;
            _Message = message.ReplaceI("chilkat", "mkryptor");
            if (errors != null)
                for (var i = 0; i < errors.Count; i++)
                {
                    errors[i] = errors[i].ReplaceI("chilkat", "mkryptor");
                    if (Message != null) _Message += "\n";
                    _Message += errors[i];
                }
            Errors = errors;
        }

        public List<string> Errors { get; set; }

        public Types Type { get; set; }

        private string _Message { get; }

        public override string Message
        {
            get { return _Message; }
        }
    }
}