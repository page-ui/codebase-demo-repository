class ErrorModel {
  final int status;
  final String errorMessage;

  ErrorModel({required this.status, required this.errorMessage});

  factory ErrorModel.fromJson(Map jsonData) {
    final rawStatus = jsonData['status'] ?? jsonData['statusCode'] ?? 0;
    final int parsedStatus = rawStatus is int
        ? rawStatus
        : int.tryParse(rawStatus.toString()) ?? 0;

    final message =
        (jsonData['Message'] ??
                jsonData['message'] ??
                jsonData['error'] ??
                'Error Occurred')
            .toString();

    return ErrorModel(status: parsedStatus, errorMessage: message);
  }

  factory ErrorModel.forOperation(String operation, {int status = 0}) {
    return ErrorModel(status: status, errorMessage: operation);
  }
}
