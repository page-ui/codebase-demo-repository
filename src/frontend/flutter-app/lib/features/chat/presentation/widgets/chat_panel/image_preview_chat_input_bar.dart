import 'package:page_ui/config/themes/app_colors.dart';
import 'package:page_ui/config/themes/app_icons.dart';
import 'package:page_ui/core/constants/borders.dart';
import 'package:page_ui/features/chat/presentation/controllers/pick_file_cubit/pick_file_cubit.dart';
import 'package:flutter/material.dart';
import 'package:flutter_bloc/flutter_bloc.dart';

class ImagePreviewChatInputBar extends StatelessWidget {
  const ImagePreviewChatInputBar({super.key});

  @override
  Widget build(BuildContext context) {
    return BlocBuilder<PickFileCubit, PickFileState>(
      builder: (context, state) {
        if (state is! PickFileSuccess) {
          return const SizedBox();
        }

        final file = state.image.files.first;
        final fileBytes = file.bytes;

        return Container(
          width: double.infinity,
          padding: const EdgeInsets.symmetric(vertical: 6, horizontal: 8),
          margin: const EdgeInsets.only(bottom: 6),
          decoration: const BoxDecoration(
            color: AppColors.lightGray,
            borderRadius: AppBorders.xxxxs,
          ),
          child: Row(
            crossAxisAlignment: CrossAxisAlignment.start,
            children: [
              ClipRRect(
                borderRadius: AppBorders.xxxxs,
                child: Container(
                  width: 56,
                  height: 56,
                  color: AppColors.black.withValues(alpha: 0.12),
                  child: fileBytes == null
                      ? const Icon(AppIcons.image, size: 18)
                      : Image.memory(fileBytes, fit: BoxFit.cover),
                ),
              ),
              const SizedBox(width: 8),

              Expanded(
                child: Column(
                  crossAxisAlignment: CrossAxisAlignment.start,
                  children: [
                    Text(
                      file.name,
                      overflow: TextOverflow.ellipsis,
                      style: const TextStyle(fontSize: 12),
                    ),
                    const SizedBox(height: 4),
                    Text(
                      'Ready to send',
                      style: TextStyle(
                        fontSize: 11,
                        color: AppColors.black.withValues(alpha: 0.65),
                      ),
                    ),
                  ],
                ),
              ),

              GestureDetector(
                onTap: () {
                  context.read<PickFileCubit>().removeImage();
                },
                child: const Icon(AppIcons.close, size: 18),
              ),
            ],
          ),
        );
      },
    );
  }
}
