using System;
using System.Runtime.Serialization;

namespace Por.Core
{
	public class PorException : Exception, ISerializable
	{
		public PorException() : base() { }
		public PorException(string message) : base(message) { }
		public PorException(string message, Exception innerException) : base(message, innerException) { }
		protected PorException(SerializationInfo info, StreamingContext context) : base(info, context) { }
	}
}