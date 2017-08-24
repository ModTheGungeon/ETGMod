using System.Diagnostics;
using UnityEngine;

namespace SGUI {
    /// <summary>
    /// Taken from https://github.com/warrenm/AHEasing/blob/master/AHEasing/easing.c,
    /// which is licensed under the "Do What The Fuck You Want To Public License, Version 2".
    /// </summary>
    public static class SEasings {

        private const float TAU = Mathf.PI * 2f;

        /// <summary>
        /// Modeled after the line y = x
        /// </summary>
        public static float Linear(float f) { return f; }

        /// <summary>
        /// Modeled after the parabola y = x^2
        /// </summary>
        public static float QuadraticEaseIn(float f) { return f * f; }

        /// <summary>
        /// Modeled after the parabola y = -x^2 + 2x
        public static float QuadraticEaseOut(float f) { return -(f * (f - 2f)); }

        /// <summary>
        /// Modeled after the piecewise quadratic
        /// y = (1/2)((2x)^2)             ; [0, 0.5)
        /// y = -(1/2)((2x-1)*(2x-3) - 1) ; [0.5, 1]
        /// </summary>
        public static float QuadraticEaseInOut(float f) {
            if (f < 0.5f) {
                return 8f * f * f * f * f;
            } else {
                f = (f - 1f);
                return -8f * f * f * f * f + 1f;
            }
        }

        /// <summary>
        /// Modeled after the cubic y = x^3
        /// </summary>
        public static float CubicEaseIn(float f) { return f * f * f; }

        /// <summary>
        /// Modeled after the cubic y = (x - 1)^3 + 1
        /// </summary>
        public static float CubicEaseOut(float f) {
            f = (f - 1f);
            return f * f * f + 1f;
        }

        /// <summary>
        /// Modeled after the piecewise cubic
        /// y = (1/2)((2x)^3)       ; [0, 0.5)
        /// y = (1/2)((2x-2)^3 + 2) ; [0.5, 1]
        /// </summary>
        public static float CubicEaseInOut(float f) {
            if (f < 0.5f) {
                return 4f * f * f * f;
            } else {
                f = ((2f * f) - 2f);
                return 0.5f * f * f * f + 1f;
            }
        }

        /// <summary>
        /// Modeled after the quartic x^4
        /// </summary>
        public static float QuarticEaseIn(float f) { return f * f * f * f; }

        /// <summary>
        /// Modeled after the quartic y = 1 - (x - 1)^4
        /// </summary>
        public static float QuarticEaseOut(float f) {
            float g = (f - 1f);
            return g * g * g * (1f - f) + 1f;
        }

        /// <summary>
        /// Modeled after the piecewise quartic
        /// y = (1/2)((2x)^4)        ; [0, 0.5)
        /// y = -(1/2)((2x-2)^4 - 2) ; [0.5, 1]
        /// </summary>
        public static float QuarticEaseInOut(float f) {
            if (f < 0.5f) {
                return 8f * f * f * f * f;
            } else {
                f = (f - 1f);
                return -8f * f * f * f * f + 1f;
            }
        }

        /// <summary>
        /// Modeled after the quintic y = x^5
        /// </summary>
        public static float QuinticEaseIn(float f) { return f * f * f * f * f; }

        /// <summary>
        /// Modeled after the quintic y = (x - 1)^5 + 1
        /// </summary>
        public static float QuinticEaseOut(float f) {
            f = (f - 1);
            return f * f * f * f * f + 1;
        }

        /// <summary>
        /// Modeled after the piecewise quintic
        /// y = (1/2)((2x)^5)       ; [0, 0.5)
        /// y = (1/2)((2x-2)^5 + 2) ; [0.5, 1]
        /// </summary>
        public static float QuinticEaseInOut(float f) {
            if (f < 0.5f) {
                return 16f * f * f * f * f * f;
            } else {
                f = ((2f * f) - 2f);
                return 0.5f * f * f * f * f * f + 1f;
            }
        }

        /// <summary>
        /// Modeled after quarter-cycle of sine wave
        /// </summary>
        public static float SineEaseIn(float f) { return Mathf.Sin((f - 1) * TAU) + 1; }

        /// <summary>
        /// Modeled after quarter-cycle of sine wave (different phase)
        /// </summary>
        public static float SineEaseOut(float f) { return Mathf.Sin(f * TAU); }

        /// <summary>
        /// Modeled after half sine wave
        /// </summary>
        public static float SineEaseInOut(float f) { return 0.5f * (1f - Mathf.Cos(f * Mathf.PI)); }

        /// <summary>
        /// Modeled after shifted quadrant IV of unit circle
        /// </summary>
        public static float CircularEaseIn(float f) { return 1f - Mathf.Sqrt(1f - (f * f)); }

        /// <summary>
        /// Modeled after shifted quadrant II of unit circle
        /// </summary>
        public static float CircularEaseOut(float f) { return Mathf.Sqrt((2f - f) * f); }

        /// <summary>
        /// Modeled after the piecewise circular function
        /// y = (1/2)(1 - sqrt(1 - 4x^2))           ; [0, 0.5)
        /// y = (1/2)(sqrt(-(2x - 3)*(2x - 1)) + 1) ; [0.5, 1]
        /// </summary>
        public static float CircularEaseInOut(float f) {
            if (f < 0.5f) {
                return 0.5f * (1f - Mathf.Sqrt(1f - 4f * (f * f)));
            } else {
                return 0.5f * (Mathf.Sqrt(-((2f * f) - 3f) * ((2f * f) - 1f)) + 1f);
            }
        }

        /// <summary>
        /// Modeled after the exponential function y = 2^(10(x - 1))
        /// </summary>
        public static float ExponentialEaseIn(float f) { return (f <= 0f) ? 0f : Mathf.Pow(2f, 10f * (f - 1f)); }

        /// <summary>
        /// Modeled after the exponential function y = -2^(-10x) + 1
        /// </summary>
        public static float ExponentialEaseOut(float f) { return (f >= 1f) ? f : 1f - Mathf.Pow(2f, -10f * f); }

        /// <summary>
        /// Modeled after the piecewise exponential
        /// y = (1/2)2^(10(2x - 1))         ; [0,0.5)
        /// y = -(1/2)*2^(-10(2x - 1))) + 1 ; [0.5,1]
        /// </summary>
        public static float ExponentialEaseInOut(float f) {
            if (f <= 0f || 1f <= f) return f;

            if (f < 0.5f) {
                return 0.5f * Mathf.Pow(2f, (20f * f) - 10f);
            } else {
                return -0.5f * Mathf.Pow(2f, (-20f * f) + 10f) + 1f;
            }
        }

        /// <summary>
        /// Modeled after the damped sine wave y = sin(13pi/2*x)*pow(2, 10 * (x - 1))
        /// </summary>
        public static float ElasticEaseIn(float f) { return Mathf.Sin(13f * TAU * f) * Mathf.Pow(2f, 10f * (f - 1f)); }

        /// <summary>
        /// Modeled after the damped sine wave y = sin(-13pi/2*(x + 1))*pow(2, -10x) + 1
        /// </summary>
        public static float ElasticEaseOut(float f) { return Mathf.Sin(-13f * TAU * (f + 1f)) * Mathf.Pow(2f, -10f * f) + 1f; }

        /// <summary>
        /// Modeled after the piecewise exponentially-damped sine wave:
        /// y = (1/2)*sin(13pi/2*(2*x))*pow(2, 10 * ((2*x) - 1))      ; [0,0.5)
        /// y = (1/2)*(sin(-13pi/2*((2x-1)+1))*pow(2,-10(2*x-1)) + 2) ; [0.5, 1]
        /// </summary>
        public static float ElasticEaseInOut(float f) {
            if (f < 0.5f) {
                return 0.5f * Mathf.Sin(13f * TAU * (2f * f)) * Mathf.Pow(2f, 10f * ((2f * f) - 1f));
            } else {
                return 0.5f * (Mathf.Sin(-13f * TAU * ((2f * f - 1f) + 1f)) * Mathf.Pow(2f, -10f * (2f * f - 1f)) + 2f);
            }
        }

        /// <summary>
        /// Modeled after the overshooting cubic y = x^3-x*sin(x*pi)
        /// </summary>
        public static float BackEaseIn(float f) { return f * f * f - f * Mathf.Sin(f * Mathf.PI); }

        /// <summary>
        /// Modeled after overshooting cubic y = 1-((1-x)^3-(1-x)*sin((1-x)*pi))
        /// </summary>
        public static float BackEaseOut(float f) {
            f = (1 - f);
            return 1f - (f * f * f - f * Mathf.Sin(f * Mathf.PI));
        }

        /// <summary>
        /// Modeled after the piecewise overshooting cubic function:
        /// y = (1/2)*((2x)^3-(2x)*sin(2*x*pi))           ; [0, 0.5)
        /// y = (1/2)*(1-((1-x)^3-(1-x)*sin((1-x)*pi))+1) ; [0.5, 1]
        /// </summary>
        public static float BackEaseInOut(float f) {
            if (f < 0.5f) {
                f = 2f * f;
                return 0.5f * (f * f * f - f * Mathf.Sin(f * Mathf.PI));
            } else {
                f = (1f - (2f * f - 1f));
                return 0.5f * (1f - (f * f * f - f * Mathf.Sin(f * Mathf.PI))) + 0.5f;
            }
        }

        public static float BounceEaseIn(float f) { return 1f - BounceEaseOut(1f - f); }

        public static float BounceEaseOut(float f) {
            if (f < 4f / 11f) {
                return (121f * f * f) / 16f;
            } else if (f < 8f / 11f) {
                return (363f / 40f * f * f) - (99f / 10f * f) + 17f / 5f;
            } else if (f < 9f / 10f) {
                return (4356f / 361f * f * f) - (35442f / 1805f * f) + 16061f / 1805f;
            } else {
                return (54f / 5f * f * f) - (513f / 25.0f * f) + 268f / 25f;
            }
        }

        public static float BounceEaseInOut(float f) {
            if (f < 0.5f) {
                return 0.5f * BounceEaseIn(f * 2f);
            } else {
                return 0.5f * BounceEaseOut(f * 2f - 1f) + 0.5f;
            }
        }

    }
}
