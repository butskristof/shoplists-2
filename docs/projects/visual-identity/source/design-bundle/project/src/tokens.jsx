// Design tokens for Shoplists — two directions
// A: Pantry — warm cream, forest green, ochre. Receipt-meets-productivity.
// B: Market — crisp white, refined teal-green, soft coral. Modern grocer.

const TOKENS = {
  pantry: {
    name: 'Pantry',
    tagline: 'A pantry-warm shopping companion',
    bg: '#F5EFE4',          // warm cream
    surface: '#FBF7EE',     // lighter cream card
    surfaceAlt: '#EEE6D4',  // deeper cream
    ink: '#1F2A20',         // deep forest-ink
    inkSoft: '#4A5249',
    muted: '#8B8471',
    rule: '#E4DBC6',        // dotted-rule warm
    primary: '#2F4A3A',     // deep forest
    primaryInk: '#F5EFE4',
    accent: '#C98A3C',      // ochre / mustard
    accentSoft: '#EDD6A8',
    berry: '#A03A3A',       // cranberry for destructive
    ok: '#5A8A5F',
    // radii & shadow
    r: 8,
    rLg: 14,
    shadow: '0 1px 0 rgba(47,74,58,0.04), 0 8px 24px -12px rgba(47,74,58,0.15)',
    font: '"Inter Tight", "Helvetica Neue", Helvetica, Arial, sans-serif',
    // `mono` is now a semantic slot — "small label" — still Inter Tight.
    // Usage sites pair it with letterSpacing + uppercase to get a refined caps feel.
    mono: '"Inter Tight", "Helvetica Neue", Helvetica, Arial, sans-serif',
  },
  market: {
    name: 'Market',
    tagline: 'Fresh lists, every week',
    bg: '#F7F8F6',          // cool near-white, faint green cast
    surface: '#FFFFFF',
    surfaceAlt: '#EEF1ED',
    ink: '#111714',         // near-black with green cast
    inkSoft: '#3D453F',
    muted: '#8A928C',
    rule: '#E4E8E3',
    primary: '#0F6B50',     // deep emerald/teal green
    primaryInk: '#FFFFFF',
    accent: '#D9603E',      // coral/persimmon
    accentSoft: '#F4D4C6',
    berry: '#B03B2E',
    ok: '#178C6A',
    r: 4,
    rLg: 10,
    shadow: '0 1px 0 rgba(15,107,80,0.04), 0 12px 28px -16px rgba(15,23,18,0.12)',
    font: '"Inter Tight", "Helvetica Neue", Helvetica, Arial, sans-serif',
    mono: '"Inter Tight", "Helvetica Neue", Helvetica, Arial, sans-serif',
    isDark: false,
  },
  marketDark: {
    name: 'Market · Dark',
    tagline: 'Fresh lists, every week',
    // Elevation via stepped surfaces: bg < surface < surfaceAlt.
    // Each step ~4% lighter. Subtle green cast preserved in the hues.
    bg: '#0D1311',          // very dark green-black
    surface: '#161D1A',     // elevated cards
    surfaceAlt: '#202825',  // hover / selected / edit-row
    ink: '#ECEFEA',         // warm near-white (not pure #FFF)
    inkSoft: '#B4BAB5',
    muted: '#7C847F',
    rule: 'rgba(255,255,255,0.08)',
    primary: '#4FB896',     // emerald lightened — legible on dark
    primaryInk: '#0B1512',  // dark ink on primary button
    accent: '#E88066',      // coral desaturated slightly
    accentSoft: 'rgba(232,128,102,0.2)',
    berry: '#E56A5E',
    ok: '#5FD1A6',
    r: 4,
    rLg: 10,
    // On dark, elevation is a subtle top highlight + no drop shadow.
    shadow: 'inset 0 1px 0 rgba(255,255,255,0.04)',
    font: '"Inter Tight", "Helvetica Neue", Helvetica, Arial, sans-serif',
    mono: '"Inter Tight", "Helvetica Neue", Helvetica, Arial, sans-serif',
    isDark: true,
  },
};

window.TOKENS = TOKENS;
