import 'dart:typed_data';

import 'package:page_ui/core/database/api/graph_ql_config.dart';
import 'package:page_ui/core/errors/app_operation.dart';
import 'package:page_ui/core/errors/error_model.dart';
import 'package:page_ui/core/errors/exceptions.dart';
import 'package:page_ui/features/chat/data/models/upload_result_model.dart';
import 'package:dio/dio.dart';

class UploadService {
  static const String _presignEndpoint = '/api/Upload/presign';
  static const AppOperation _operation = AppOperation.upload;

  final Dio _client;

  UploadService({Dio? client}) : _client = client ?? GraphQLConfig.restClient;

  Future<String> upload({
    required Uint8List fileBytes,
    required String fileName,
    required String contentType,
  }) async {
    final presignResult = await _getPresignedUrl(fileName);
    await _uploadBinary(
      uploadUrl: _resolveAgainstBase(presignResult.uploadUrl),
      fileBytes: fileBytes,
      contentType: contentType,
    );
    return _resolveAgainstBase(presignResult.downloadUrl);
  }

  String _resolveAgainstBase(String url) {
    final parsed = Uri.parse(url);
    if (parsed.hasScheme) return url;
    return Uri.parse(GraphQLConfig.uri).resolveUri(parsed).toString();
  }

  Future<UploadResultModel> _getPresignedUrl(String originalFileName) async {
    final baseUri = Uri.parse(GraphQLConfig.uri);
    final presignUri = baseUri.replace(
      path: _presignEndpoint,
      queryParameters: {'fileName': originalFileName},
    );

    try {
      final response = await _client.get(presignUri.toString());

      if (response.statusCode != 200) {
        throw BadResponseException(
          ErrorModel(
            status: response.statusCode ?? 0,
            errorMessage: _operation.name,
          ),
          operation: _operation,
        );
      }

      return UploadResultModel.fromJson(response.data as Map<String, dynamic>);
    } on DioException catch (e) {
      throw ServerException.fromDio(e, operation: _operation);
    }
  }

  Future<void> _uploadBinary({
    required String uploadUrl,
    required Uint8List fileBytes,
    required String contentType,
  }) async {
    if (uploadUrl.isEmpty || fileBytes.isEmpty) {
      throw BadResponseException(
        ErrorModel(status: 0, errorMessage: _operation.name),
        operation: _operation,
      );
    }

    try {
      final uploadClient = Dio();
      final response = await uploadClient.put(
        uploadUrl,
        data: fileBytes,
        options: Options(
          contentType: contentType,
          headers: {Headers.contentLengthHeader: fileBytes.length},
        ),
      );

      if (response.statusCode != 200 && response.statusCode != 204) {
        throw BadResponseException(
          ErrorModel(
            status: response.statusCode ?? 0,
            errorMessage: _operation.name,
          ),
          operation: _operation,
        );
      }
    } on DioException catch (e) {
      throw ServerException.fromDio(e, operation: _operation);
    }
  }
}
