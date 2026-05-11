import 'package:web/web.dart' as web;

void openUiUrlInBrowser(String url) {
  final trimmedUrl = url.trim();

  if (trimmedUrl.isEmpty) return;

  final encodedUrl = Uri.encodeComponent(trimmedUrl);

  web.window.open(
    '/preview.html?url=$encodedUrl',
    '_blank',
  );
}