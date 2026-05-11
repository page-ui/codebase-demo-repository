import 'package:page_ui/core/errors/error_model.dart';
import 'package:page_ui/core/errors/exceptions.dart';
import 'package:graphql_flutter/graphql_flutter.dart' hide ServerException;
import 'package:logger/logger.dart';


class GraphQLLoggerLink extends Link {
  final Logger _logger = Logger(
    printer: PrettyPrinter(methodCount: 0, noBoxingByDefault: false),
  );

  @override
  Stream<Response> request(Request request, [NextLink? forward]) {
    final String operationName =
        request.operation.operationName ?? "Unnamed Operation";

    
    _logger.i(
      '🚀 GRAPHQL REQUEST: $operationName\n'
      'Variables: ${request.variables}',
    );

    return forward!(request)
        .map((response) {
          
          if (response.errors != null && response.errors!.isNotEmpty) {
            _logger.e(
              '❌ GRAPHQL ERRORS [$operationName]:\n'
              '${response.errors.toString()}',
            );
          } else {
            _logger.d(
              '✅ GRAPHQL RESPONSE [$operationName]:\n'
              'Data: ${response.data}',
            );
          }

          return response;
        })
        .handleError((Object error) {
          
          _logger.f('🚨 GRAPHQL TERMINAL ERROR [$operationName]:\n$error');
          if (error is ServerException) {
            throw error;
          }
          throw ConnectionErrorException(
            ErrorModel(status: 0, errorMessage: operationName),
          );
        });
  }
}
