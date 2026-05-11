import 'dart:math';

import 'package:flutter/material.dart';

class _Star {
  double x;
  double y;
  final double size;
  final double speed;
  final double opacity;
  final double twinkleSpeed;
  double twinklePhase;

  _Star({
    required this.x,
    required this.y,
    required this.size,
    required this.speed,
    required this.opacity,
    required this.twinkleSpeed,
    required this.twinklePhase,
  });
}

class _Nebula {
  final double x;
  final double y;
  final double radius;
  final Color color;
  final double driftSpeedX;
  final double driftSpeedY;

  _Nebula({
    required this.x,
    required this.y,
    required this.radius,
    required this.color,
    required this.driftSpeedX,
    required this.driftSpeedY,
  });
}

class AnimatedStarfieldBackground extends StatefulWidget {
  const AnimatedStarfieldBackground({super.key, required this.child});

  final Widget child;

  @override
  State<AnimatedStarfieldBackground> createState() =>
      _AnimatedStarfieldBackgroundState();
}

class _AnimatedStarfieldBackgroundState
    extends State<AnimatedStarfieldBackground>
    with SingleTickerProviderStateMixin {
  late final AnimationController _controller;
  final List<_Star> _stars = [];
  final List<_Nebula> _nebulae = [];
  final Random _random = Random(42);
  bool _initialized = false;

  static const int _starCount = 150;
  static const int _nebulaCount = 3;

  @override
  void initState() {
    super.initState();
    _controller = AnimationController(
      vsync: this,
      duration: const Duration(seconds: 1),
    )..repeat();
  }

  void _initParticles() {
    if (_initialized) return;
    _initialized = true;

    for (int i = 0; i < _starCount; i++) {
      _stars.add(
        _Star(
          x: _random.nextDouble(),
          y: _random.nextDouble(),
          size: _random.nextDouble() * 2.2 + 0.3,
          speed: (_random.nextDouble() * 0.15 + 0.02) * 0.001,
          opacity: _random.nextDouble() * 0.7 + 0.3,
          twinkleSpeed: _random.nextDouble() * 2.5 + 0.5,
          twinklePhase: _random.nextDouble() * pi * 2,
        ),
      );
    }

    final nebulaColors = [
      const Color(0xFF539062).withValues(alpha: 0.04),
      const Color(0xFF1E88E5).withValues(alpha: 0.03),
      const Color(0xFF7C4DFF).withValues(alpha: 0.025),
    ];
    for (int i = 0; i < _nebulaCount; i++) {
      _nebulae.add(
        _Nebula(
          x: _random.nextDouble(),
          y: _random.nextDouble(),
          radius: _random.nextDouble() * 250 + 150,
          color: nebulaColors[i % nebulaColors.length],
          driftSpeedX: (_random.nextDouble() - 0.5) * 0.3 * 0.005,
          driftSpeedY: (_random.nextDouble() - 0.5) * 0.2 * 0.005,
        ),
      );
    }
  }

  @override
  void dispose() {
    _controller.dispose();
    super.dispose();
  }

  @override
  Widget build(BuildContext context) {
    return LayoutBuilder(
      builder: (context, constraints) {
        final size = Size(constraints.maxWidth, constraints.maxHeight);
        _initParticles();

        return Directionality(
          textDirection: TextDirection.ltr,
          child: Stack(
            children: [
              Container(color: const Color(0xFF0A0A0F)),

              AnimatedBuilder(
                animation: _controller,
                builder: (context, _) {
                  return CustomPaint(
                    size: size,
                    painter: _StarfieldPainter(
                      stars: _stars,
                      nebulae: _nebulae,
                      time: DateTime.now().millisecondsSinceEpoch / 1000.0,
                      screenSize: size,
                    ),
                  );
                },
              ),

              Container(
                decoration: BoxDecoration(
                  gradient: RadialGradient(
                    center: Alignment.center,
                    radius: 1.2,
                    colors: [
                      Colors.transparent,
                      const Color(0xFF0A0A0F).withValues(alpha: 0.6),
                    ],
                  ),
                ),
              ),

              widget.child,
            ],
          ),
        );
      },
    );
  }
}

class _StarfieldPainter extends CustomPainter {
  final List<_Star> stars;
  final List<_Nebula> nebulae;
  final double time;
  final Size screenSize;

  _StarfieldPainter({
    required this.stars,
    required this.nebulae,
    required this.time,
    required this.screenSize,
  });

  @override
  void paint(Canvas canvas, Size size) {
    if (size.width <= 0 || size.height <= 0) return;

    for (final nebula in nebulae) {
      double relX = (nebula.x + nebula.driftSpeedX * time) % 1.0;
      if (relX < 0) relX += 1.0;
      double relY = (nebula.y + nebula.driftSpeedY * time) % 1.0;
      if (relY < 0) relY += 1.0;

      final nx = relX * size.width;
      final ny = relY * size.height;

      final paint = Paint()
        ..shader = RadialGradient(colors: [nebula.color, Colors.transparent])
            .createShader(
              Rect.fromCircle(center: Offset(nx, ny), radius: nebula.radius),
            );

      canvas.drawCircle(Offset(nx, ny), nebula.radius, paint);
    }

    for (final star in stars) {
      star.y = (star.y + star.speed) % 1.0;
      star.x = (star.x + star.speed * 0.3) % 1.0;

      final sx = star.x * size.width;
      final sy = star.y * size.height;

      final twinkle =
          (sin(time * star.twinkleSpeed + star.twinklePhase) + 1) / 2;
      final alpha = (star.opacity * (0.4 + 0.6 * twinkle)).clamp(0.0, 1.0);

      final paint = Paint()..color = Color.fromRGBO(255, 255, 255, alpha);

      if (star.size > 1.5) {
        final glowPaint = Paint()
          ..color = Color.fromRGBO(255, 255, 255, alpha * 0.2)
          ..maskFilter = const MaskFilter.blur(BlurStyle.normal, 4);
        canvas.drawCircle(Offset(sx, sy), star.size * 2, glowPaint);
      }

      canvas.drawCircle(Offset(sx, sy), star.size, paint);
    }
  }

  @override
  bool shouldRepaint(covariant _StarfieldPainter oldDelegate) => true;
}
