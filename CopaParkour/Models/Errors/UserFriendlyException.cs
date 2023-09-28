using System.Net;

namespace CopaParkour.Models.Errors; 
public class UserFriendlyException : Exception {

	public HttpStatusCode Status { get; protected set; }

	public UserFriendlyException(string message) : base(message) {
		this.Status = HttpStatusCode.InternalServerError;
	}


	public UserFriendlyException(string message, HttpStatusCode statusCode) : base(message) {
		this.Status = statusCode;
	}

}

public class UserFriendlyException<T> : UserFriendlyException where T : Enum {
	
	public T Value { get; protected set; }
	
	public UserFriendlyException(string message) : base(message)
	{
	}
	public UserFriendlyException(string message, HttpStatusCode statusCode) : base(message, statusCode)
	{
	}

	public UserFriendlyException(string message, T value) : base(message)
	{
		this.Value = value;
	}
	public UserFriendlyException(string message, T value, HttpStatusCode statusCode) : base(message, statusCode)
	{
		this.Value = value;
	}

}