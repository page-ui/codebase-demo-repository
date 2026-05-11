String encodeMessageContent(String content) {
  return content
      .replaceAll('\r\n', '\n')
      .replaceAll('\r', '\n')
      .replaceAll('\n', r'\n');
}

String decodeMessageContent(String content) {
  return content.replaceAll(r'\n', '\n');
}
