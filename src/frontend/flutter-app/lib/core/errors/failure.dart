import 'package:page_ui/core/errors/app_operation.dart';
import 'package:page_ui/core/errors/exceptions.dart';

class Failure {
  final String message;

  Failure({required this.message});
}

class ServerFailure extends Failure {
  ServerFailure({required super.message});

  factory ServerFailure.fromServer(int? statusCode) {
    if (statusCode == 401) {
      return ServerFailure(
        message: "Unauthorized access. Please check your credentials.",
      );
    }
    return ServerFailure(message: _defaultFriendly);
  }

  factory ServerFailure.fromException(ServerException exception) {
    final operation = exception.operation;
    final status = exception.errorModel.status;

    if (exception is ConnectionTimeoutException ||
        exception is SendTimeoutException ||
        exception is ReceiveTimeoutException) {
      return ServerFailure(
        message: "The server is taking too long to respond. Please try again.",
      );
    }

    if (exception is ConnectionErrorException ||
        exception is BadCertificateException) {
      return ServerFailure(
        message:
            "We couldn't reach the server. Please check your connection and try again.",
      );
    }

    if (exception is CancelException) {
      return ServerFailure(
        message: "The request was cancelled. Please try again.",
      );
    }

    return ServerFailure(message: _messageFor(operation, status, exception));
  }

  factory ServerFailure.forOperation(AppOperation operation, {int status = 0}) {
    return ServerFailure(message: _messageFor(operation, status, null));
  }

  static String _messageFor(
    AppOperation operation,
    int status,
    ServerException? exception,
  ) {
    final overrides = _statusOverrides[operation];
    if (overrides != null && overrides.containsKey(status)) {
      return overrides[status]!;
    }

    if (exception is UnauthorizedException) {
      return _unauthorizedFor(operation);
    }
    if (exception is ForbiddenException) {
      return "You do not have permission to do that.";
    }
    if (exception is NotFoundException) {
      return _notFoundFor(operation);
    }

    if (exception is BadResponseException &&
        exception.errorModel.errorMessage.isNotEmpty &&
        exception.errorModel.errorMessage != operation.name) {
      return exception.errorModel.errorMessage;
    }

    return _operationDefaults[operation] ?? _defaultFriendly;
  }

  static String _unauthorizedFor(AppOperation operation) {
    switch (operation) {
      case AppOperation.login:
        return "Your email or password is incorrect. Please try again.";
      default:
        return "Your session has expired. Please sign in again.";
    }
  }

  static String _notFoundFor(AppOperation operation) {
    switch (operation) {
      case AppOperation.loadChats:
      case AppOperation.searchChats:
      case AppOperation.loadMessages:
        return "We could not find what you were looking for.";
      default:
        return "The requested item was not found.";
    }
  }
}

class NetworkFailure extends Failure {
  NetworkFailure({required super.message});
  factory NetworkFailure.error() {
    return NetworkFailure(
      message: "No internet connection. Please connect to a network.",
    );
  }
}

class CacheFailure extends Failure {
  CacheFailure({required super.message});
  factory CacheFailure.fromException(CacheExeption exception) {
    return CacheFailure(
      message: "We couldn't load your saved data. Please try again.",
    );
  }
}

const String _defaultFriendly =
    "Something went wrong. Please try again in a moment.";

const Map<AppOperation, String> _operationDefaults = {
  AppOperation.login:
      "We couldn't sign you in. Please check your email and password and try again.",
  AppOperation.register:
      "We couldn't create your account. The email might already be in use.",
  AppOperation.forgotPassword:
      "We couldn't start a password reset. Please check your email and try again.",
  AppOperation.verifyCode:
      "The code you entered didn't match. Please try again or request a new code.",
  AppOperation.verifyEmail:
      "The code you entered didn't match. Please try again or request a new code.",
  AppOperation.resetPassword:
      "We couldn't change your password. Please try again.",
  AppOperation.resendCode: "We couldn't resend the code. Please try again.",
  AppOperation.createChat: "We couldn't create your chat. Please try again.",
  AppOperation.loadChats: "We couldn't load your chats. Please try again.",
  AppOperation.searchChats: "We couldn't search your chats. Please try again.",
  AppOperation.loadMessages:
      "We couldn't load your messages. Please try again.",
  AppOperation.subscribeMessages:
      "Live updates were interrupted. Please reopen the chat.",
  AppOperation.sendMessage: "We couldn't send your message. Please try again.",
  AppOperation.deleteChat: "We couldn't delete this chat. Please try again.",
  AppOperation.renameChat: "We couldn't rename this chat. Please try again.",
  AppOperation.upload:
      "We couldn't upload your file. Please try a different file or try again.",
  AppOperation.parseMessage:
      "A message couldn't be displayed. Please reload the chat.",
  AppOperation.signOut: "We couldn't sign you out. Please try again.",
  AppOperation.deleteAccount:
      "We couldn't delete your account. Please try again.",
  AppOperation.generic: _defaultFriendly,
};

const Map<AppOperation, Map<int, String>> _statusOverrides = {
  AppOperation.login: {
    401: "Your email or password is incorrect. Please try again.",
    403: "Your account is not allowed to sign in.",
    429: "Too many sign-in attempts. Please wait a moment and try again.",
  },
  AppOperation.register: {
    409: "An account with this email already exists.",
    422: "Please check your details and try again.",
  },
  AppOperation.forgotPassword: {404: "No account matches that email."},
  AppOperation.verifyCode: {
    400: "The code you entered is invalid or has expired.",
    410: "This code has expired. Please request a new one.",
  },
  AppOperation.verifyEmail: {
    400: "The code you entered is invalid or has expired.",
    410: "This code has expired. Please request a new one.",
  },
  AppOperation.upload: {
    413: "This file is too large to upload.",
    415: "This file type is not supported.",
  },
  AppOperation.sendMessage: {413: "Your message is too long."},
  AppOperation.renameChat: {409: "A chat with that name already exists."},
};
