class ChatEntity {
  final String id;
  final String name;
  final DateTime? createdAt;

  const ChatEntity({required this.id, required this.name, this.createdAt});
}
