import 'package:flutter/material.dart';

class AppBreakpoints {
  static const double mobile = 700;
  static const double tablet = 1050;
  static const double desktop = 1400;
}

enum ScreenType { mobile, tablet, desktop, largeDesktop }

class ResponsiveHelper {
  static ScreenType getScreenType(double width) {
    if (width < AppBreakpoints.mobile) {
      return ScreenType.mobile;
    } else if (width < AppBreakpoints.tablet) {
      return ScreenType.tablet;
    } else if (width < AppBreakpoints.desktop) {
      return ScreenType.desktop;
    } else {
      return ScreenType.largeDesktop;
    }
  }
}

extension ResponsiveExtension on BuildContext {
  double get screenWidth => MediaQuery.of(this).size.width;

  ScreenType get screenType => ResponsiveHelper.getScreenType(screenWidth);

  bool get isMobile => screenType == ScreenType.mobile;
  bool get isTablet => screenType == ScreenType.tablet;
  bool get isDesktop =>
      screenType == ScreenType.desktop || screenType == ScreenType.largeDesktop;
  bool get isLargeDesktop => screenType == ScreenType.largeDesktop;

  double responsiveValue({
    required double mobile,
    double? tablet,
    double? desktop,
  }) {
    switch (screenType) {
      case ScreenType.mobile:
        return mobile;
      case ScreenType.tablet:
        return tablet ?? mobile;
      case ScreenType.desktop:
      case ScreenType.largeDesktop:
        return desktop ?? tablet ?? mobile;
    }
  }
}
