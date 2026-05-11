class UploadResultModel {
  final String uploadUrl;
  final String downloadUrl;
  final String fileName;

  UploadResultModel({
    required this.uploadUrl,
    required this.downloadUrl,
    required this.fileName,
  });

  factory UploadResultModel.fromJson(Map<String, dynamic> json) {
    return UploadResultModel(
      uploadUrl: json['uploadUrl'] as String,
      downloadUrl: json['downloadUrl'] as String,
      fileName: json['fileName'] as String,
    );
  }
}
