namespace Adfitech
{
    /// <summary>
    ///     Throws when known, JSON formatted errors are returned from the integration service.
    /// </summary>
    class IntegrationException : System.Exception
    {
        public IntegrationException() : base() { }
        public IntegrationException(string message) : base(message) { }
        public IntegrationException(string message, System.Exception inner) : base(message, inner) { }
        protected IntegrationException(System.Runtime.Serialization.SerializationInfo info,
            System.Runtime.Serialization.StreamingContext context) { }

        public CollectionJSON.Error error { get; set; }

        public IntegrationException(string message, CollectionJSON.Error _error)
            : base(message)
        {
            error = _error;
        }
    }

    /// <summary>
    ///     Thrown when HTTP 404 is received from the integration service along with a
    ///     de-serializable JSON message.
    /// </summary>
    class ResourceNotFoundException : Adfitech.IntegrationException
    {
        public ResourceNotFoundException() : base() { }
        public ResourceNotFoundException(string message) : base(message) { }
        public ResourceNotFoundException(string message, System.Exception inner) : base(message, inner) { }
        protected ResourceNotFoundException(System.Runtime.Serialization.SerializationInfo info,
            System.Runtime.Serialization.StreamingContext context) { }
        public ResourceNotFoundException(string message, CollectionJSON.Error _error) : base(message) { }
    }

    /// <summary>
    ///     Thrown when HTTP 409 is received from the integration service along with a
    ///     de-serializable JSON message.
    /// </summary>
    class ResourceNotReadyException : Adfitech.IntegrationException
    {
        public ResourceNotReadyException() : base() { }
        public ResourceNotReadyException(string message) : base(message) { }
        public ResourceNotReadyException(string message, System.Exception inner) : base(message, inner) { }
        protected ResourceNotReadyException(System.Runtime.Serialization.SerializationInfo info,
            System.Runtime.Serialization.StreamingContext context) { }
        public ResourceNotReadyException(string message, CollectionJSON.Error _error) : base(message) { }
    }
}