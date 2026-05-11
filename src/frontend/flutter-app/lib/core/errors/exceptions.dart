import 'package:page_ui/core/errors/app_operation.dart';
import 'package:page_ui/core/errors/error_model.dart';
import 'package:dio/dio.dart';
import 'package:graphql_flutter/graphql_flutter.dart';

class ServerException implements Exception {
  final ErrorModel errorModel;
  final AppOperation operation;
  ServerException(this.errorModel, {this.operation = AppOperation.generic});

  factory ServerException.fromGraphQL(
    OperationException? exception, {
    required AppOperation operation,
  }) {
    if (exception == null) {
      return UnknownException(
        ErrorModel(status: 0, errorMessage: operation.name),
        operation: operation,
      );
    }

    final linkException = exception.linkException;
    if (linkException is NetworkException) {
      return ConnectionErrorException(
        ErrorModel(status: 0, errorMessage: operation.name),
        operation: operation,
      );
    }

    final graphqlErrors = exception.graphqlErrors;
    if (graphqlErrors.isNotEmpty) {
      final first = graphqlErrors.first;
      final code = first.extensions?['code']?.toString() ?? '';
      final statusRaw = first.extensions?['statusCode'];
      final status = statusRaw is int
          ? statusRaw
          : int.tryParse(statusRaw?.toString() ?? '') ?? _codeToStatus(code);
      final errorMessage = first.message.isNotEmpty ? first.message : operation.name;
      final model = ErrorModel(status: status, errorMessage: errorMessage);

      switch (code) {
        case 'AUTH_NOT_AUTHENTICATED':
        case 'UNAUTHENTICATED':
          return UnauthorizedException(model, operation: operation);
        case 'FORBIDDEN':
          return ForbiddenException(model, operation: operation);
        case 'NOT_FOUND':
          return NotFoundException(model, operation: operation);
        case 'BAD_USER_INPUT':
        case 'BAD_REQUEST':
          return BadResponseException(model, operation: operation);
      }

      switch (status) {
        case 401:
          return UnauthorizedException(model, operation: operation);
        case 403:
          return ForbiddenException(model, operation: operation);
        case 404:
          return NotFoundException(model, operation: operation);
      }

      return BadResponseException(model, operation: operation);
    }

    return UnknownException(
      ErrorModel(status: 0, errorMessage: operation.name),
      operation: operation,
    );
  }

  factory ServerException.fromDio(
    DioException exception, {
    required AppOperation operation,
  }) {
    final status = exception.response?.statusCode ?? 0;
    final model = ErrorModel(status: status, errorMessage: operation.name);

    switch (exception.type) {
      case DioExceptionType.connectionTimeout:
        return ConnectionTimeoutException(model, operation: operation);
      case DioExceptionType.sendTimeout:
        return SendTimeoutException(model, operation: operation);
      case DioExceptionType.receiveTimeout:
        return ReceiveTimeoutException(model, operation: operation);
      case DioExceptionType.badCertificate:
        return BadCertificateException(model, operation: operation);
      case DioExceptionType.cancel:
        return CancelException(model, operation: operation);
      case DioExceptionType.connectionError:
        return ConnectionErrorException(model, operation: operation);
      case DioExceptionType.badResponse:
        if (status == 401) {
          return UnauthorizedException(model, operation: operation);
        }
        if (status == 403) {
          return ForbiddenException(model, operation: operation);
        }
        if (status == 404) {
          return NotFoundException(model, operation: operation);
        }
        return BadResponseException(model, operation: operation);
      case DioExceptionType.unknown:
        return UnknownException(model, operation: operation);
    }
  }

  factory ServerException.forOperation(
    AppOperation operation, {
    int status = 0,
  }) {
    return ServerException(
      ErrorModel(status: status, errorMessage: operation.name),
      operation: operation,
    );
  }
}

int _codeToStatus(String code) {
  switch (code) {
    case 'UNAUTHENTICATED':
    case 'AUTH_NOT_AUTHENTICATED':
      return 401;
    case 'FORBIDDEN':
      return 403;
    case 'NOT_FOUND':
      return 404;
    case 'BAD_USER_INPUT':
    case 'BAD_REQUEST':
      return 400;
    case 'CONFLICT':
      return 409;
    default:
      return 0;
  }
}

class CacheExeption implements Exception {
  final String errorMessage;
  CacheExeption({required this.errorMessage});
}

class BadCertificateException extends ServerException {
  BadCertificateException(super.errorModel, {super.operation});
}

class ConnectionTimeoutException extends ServerException {
  ConnectionTimeoutException(super.errorModel, {super.operation});
}

class BadResponseException extends ServerException {
  BadResponseException(super.errorModel, {super.operation});
}

class ReceiveTimeoutException extends ServerException {
  ReceiveTimeoutException(super.errorModel, {super.operation});
}

class ConnectionErrorException extends ServerException {
  ConnectionErrorException(super.errorModel, {super.operation});
}

class SendTimeoutException extends ServerException {
  SendTimeoutException(super.errorModel, {super.operation});
}

class UnauthorizedException extends ServerException {
  UnauthorizedException(super.errorModel, {super.operation});
}

class ForbiddenException extends ServerException {
  ForbiddenException(super.errorModel, {super.operation});
}

class NotFoundException extends ServerException {
  NotFoundException(super.errorModel, {super.operation});
}

class CofficientException extends ServerException {
  CofficientException(super.errorModel, {super.operation});
}

class CancelException extends ServerException {
  CancelException(super.errorModel, {super.operation});
}

class UnknownException extends ServerException {
  UnknownException(super.errorModel, {super.operation});
}
