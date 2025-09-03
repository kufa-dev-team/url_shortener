namespace Domain.Result;

public enum ErrorCode {
    NOT_FOUND=404,
    BAD_REQUEST=400,
    UNAUTHORIZED=401,
    FORBIDDEN=403,
    INTERNAL_SERVER_ERROR=500,
}

public class Error {
    public string message;
    public ErrorCode code;
    public Error(string message, ErrorCode code) {
        this.message = message;
        this.code = code;
    }
}