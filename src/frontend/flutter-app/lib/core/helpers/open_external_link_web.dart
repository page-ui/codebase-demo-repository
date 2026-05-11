import 'package:web/web.dart' as web;

void openExternalLink(String url) {
  final trimmedUrl = url.trim();

  if (trimmedUrl.isEmpty) return;

  web.window.open(trimmedUrl, '_blank');
}
