namespace Domain.Result;

public interface Result<T> {
    bool is_success();
}

public class Success<T> : Result<T> {
    public T res;
    public Success(T res) {
        this.res = res;
    }
    public bool is_success() {return true;}
}

public class Failure<T> : Result<T> {
    public Error error;
    public Failure(Error error) {
        this.error = error;
    }
    public bool is_success() {return false;}
}
