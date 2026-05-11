import 'package:page_ui/config/themes/app_colors.dart';
import 'package:page_ui/config/themes/app_icons.dart';
import 'package:page_ui/config/themes/app_text_style.dart';
import 'package:flutter/material.dart';

class HistorySearchTextField extends StatelessWidget {
  const HistorySearchTextField({
    super.key,
    required this.searchController,
    required this.onSearch,
  });
  final searchController;
  final ValueChanged<String> onSearch;
  @override
  Widget build(BuildContext context) {
    return TextField(
      controller: searchController,
      onChanged: onSearch,
      style: AppTextStyles.bodyMedium!.copyWith(
        color: AppColors.white.withValues(alpha: 0.9),
        fontSize: 13,
      ),
      decoration: InputDecoration(
        hintText: 'Search chats...',
        hintStyle: AppTextStyles.bodyMedium!.copyWith(
          color: AppColors.lightGray.withValues(alpha: 0.4),
          fontSize: 13,
        ),
        prefixIcon: Icon(
          AppIcons.search,
          size: 18,
          color: AppColors.lightGray.withValues(alpha: 0.5),
        ),
        filled: true,
        fillColor: AppColors.black.withValues(alpha: 0.3),
        contentPadding: const EdgeInsets.symmetric(horizontal: 12, vertical: 8),
        border: OutlineInputBorder(
          borderRadius: BorderRadius.circular(8),
          borderSide: BorderSide(
            color: AppColors.darkGrey.withValues(alpha: 0.3),
          ),
        ),
        enabledBorder: OutlineInputBorder(
          borderRadius: BorderRadius.circular(8),
          borderSide: BorderSide(
            color: AppColors.darkGrey.withValues(alpha: 0.3),
          ),
        ),
        focusedBorder: OutlineInputBorder(
          borderRadius: BorderRadius.circular(8),
          borderSide: BorderSide(
            color: AppColors.primaryColor.withValues(alpha: 0.5),
          ),
        ),
        isDense: true,
      ),
    );
  }
}
