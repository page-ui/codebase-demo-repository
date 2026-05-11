import 'dart:async';

import 'package:page_ui/config/themes/app_colors.dart';
import 'package:page_ui/core/constants/borders.dart';
import 'package:page_ui/features/chat/domain/constants/message_types.dart';
import 'package:page_ui/features/chat/domain/entities/message_entity.dart';
import 'package:page_ui/features/chat/presentation/controllers/chat_messages_cubit/chat_typewriter_registry.dart';
import 'package:flutter/material.dart';

class ChatMessageContent extends StatelessWidget {
  const ChatMessageContent({super.key, required this.message});

  final MessageEntity message;

  bool get _isAiRun => isAiRunType(message.type);

  bool get _isAssistant => isAssistantMessage(message.type);

  bool get _hasImage =>
      !_isAssistant &&
      message.attachmentUrl != null &&
      message.attachmentUrl!.trim().isNotEmpty;

  String? get _displayText {
    final content = message.content.trim();
    if (_isAiRun) {
      return 'The UI';
    }

    if (content.isEmpty) return null;
    if (_hasImage && content.toLowerCase() == 'image') return null;
    return content;
  }

  @override
  Widget build(BuildContext context) {
    return Column(
      crossAxisAlignment: CrossAxisAlignment.start,
      children: [
        if (!_isAssistant)
          _ChatMessageImage(
            imageUrl: message.attachmentUrl,
            heroTag: 'chat-image-${message.id}',
          ),
        if (_hasImage && _displayText != null) const SizedBox(height: 8),
        _ChatMessageText(
          content: _displayText,
          messageId: message.id,
          animate: _isAssistant,
        ),
      ],
    );
  }
}

class _ChatMessageImage extends StatelessWidget {
  const _ChatMessageImage({required this.imageUrl, required this.heroTag});

  final String? imageUrl;
  final String heroTag;

  @override
  Widget build(BuildContext context) {
    final url = imageUrl?.trim();
    if (url == null || url.isEmpty) {
      return const SizedBox.shrink();
    }

    return GestureDetector(
      onTap: () {
        Navigator.of(context).push(
          PageRouteBuilder(
            pageBuilder: (_, __, ___) =>
                _FullScreenImageViewer(imageUrl: url, heroTag: heroTag),
            transitionsBuilder:
                (context, animation, secondaryAnimation, child) {
                  return FadeTransition(opacity: animation, child: child);
                },
          ),
        );
      },
      child: ClipRRect(
        borderRadius: AppBorders.xxxxs,
        child: Hero(
          tag: heroTag,
          child: Image.network(
            url,
            width: 310,
            height: 260,
            fit: BoxFit.cover,
            loadingBuilder: (context, child, loadingProgress) {
              if (loadingProgress == null) return child;
              return Container(
                width: 310,
                height: 260,
                color: AppColors.black.withValues(alpha: 0.2),
                alignment: Alignment.center,
                child: CircularProgressIndicator(
                  strokeWidth: 2,
                  color: AppColors.white.withValues(alpha: 0.8),
                ),
              );
            },
            errorBuilder: (context, error, stackTrace) {
              return Container(
                width: 310,
                height: 260,
                padding: const EdgeInsets.all(12),
                color: AppColors.black.withValues(alpha: 0.2),
                alignment: Alignment.center,
                child: Text(
                  'Failed to load image',
                  style: TextStyle(
                    color: AppColors.white.withValues(alpha: 0.8),
                    fontSize: 12,
                  ),
                ),
              );
            },
          ),
        ),
      ),
    );
  }
}

class _ChatMessageText extends StatefulWidget {
  const _ChatMessageText({
    required this.content,
    required this.messageId,
    required this.animate,
  });

  final String? content;
  final String messageId;
  final bool animate;

  @override
  State<_ChatMessageText> createState() => _ChatMessageTextState();
}

class _ChatMessageTextState extends State<_ChatMessageText> {
  static const Duration _tickInterval = Duration(milliseconds: 18);

  Timer? _timer;
  String _visible = '';
  String _fullText = '';
  int _cursor = 0;

  @override
  void initState() {
    super.initState();
    _fullText = widget.content?.trim() ?? '';
    final shouldAnimate =
        widget.animate &&
        _fullText.isNotEmpty &&
        ChatTypewriterRegistry.shouldAnimate(widget.messageId);
    if (shouldAnimate) {
      _visible = '';
      _cursor = 0;
      _startTyping();
    } else {
      _visible = _fullText;
      _cursor = _fullText.length;
    }
  }

  @override
  void didUpdateWidget(covariant _ChatMessageText oldWidget) {
    super.didUpdateWidget(oldWidget);
    final next = widget.content?.trim() ?? '';
    if (next == _fullText) return;
    _fullText = next;
    if (_timer == null) {
      _visible = _fullText;
      _cursor = _fullText.length;
      if (mounted) setState(() {});
    } else if (_cursor > _fullText.length) {
      _cursor = _fullText.length;
      _visible = _fullText;
    }
  }

  void _startTyping() {
    _timer = Timer.periodic(_tickInterval, (timer) {
      if (!mounted) {
        timer.cancel();
        return;
      }
      if (_cursor >= _fullText.length) {
        timer.cancel();
        _timer = null;
        ChatTypewriterRegistry.markCompleted(widget.messageId);
        return;
      }
      setState(() {
        _cursor += 1;
        _visible = _fullText.substring(0, _cursor);
      });
    });
  }

  @override
  void dispose() {
    _timer?.cancel();
    super.dispose();
  }

  @override
  Widget build(BuildContext context) {
    if (_fullText.isEmpty) {
      return const SizedBox.shrink();
    }
    return Text(
      _visible,
      style: TextStyle(
        color: AppColors.white.withValues(alpha: 0.9),
        fontSize: 14,
        height: 1.5,
      ),
    );
  }
}

class _FullScreenImageViewer extends StatelessWidget {
  const _FullScreenImageViewer({required this.imageUrl, required this.heroTag});

  final String imageUrl;
  final String heroTag;

  @override
  Widget build(BuildContext context) {
    return Scaffold(
      backgroundColor: AppColors.black,
      body: SafeArea(
        child: Stack(
          children: [
            Center(
              child: InteractiveViewer(
                minScale: 0.8,
                maxScale: 4,
                child: Hero(
                  tag: heroTag,
                  child: Image.network(
                    imageUrl,
                    fit: BoxFit.contain,
                    loadingBuilder: (context, child, loadingProgress) {
                      if (loadingProgress == null) return child;
                      return CircularProgressIndicator(
                        strokeWidth: 2,
                        color: AppColors.white.withValues(alpha: 0.8),
                      );
                    },
                    errorBuilder: (context, error, stackTrace) {
                      return Text(
                        'Failed to load image',
                        style: TextStyle(
                          color: AppColors.white.withValues(alpha: 0.8),
                          fontSize: 14,
                        ),
                      );
                    },
                  ),
                ),
              ),
            ),
            Positioned(
              top: 12,
              right: 12,
              child: IconButton(
                onPressed: () => Navigator.of(context).pop(),
                icon: const Icon(Icons.close_rounded),
                color: AppColors.white,
                style: IconButton.styleFrom(
                  backgroundColor: AppColors.black.withValues(alpha: 0.45),
                ),
              ),
            ),
          ],
        ),
      ),
    );
  }
}
