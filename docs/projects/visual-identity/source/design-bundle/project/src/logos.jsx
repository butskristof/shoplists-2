// Shoplists brand marks — two versions matching the two directions.

// Pantry mark — a hand-drawn basket with "Shoplists" in tight sans.
// The feel: farmer's market chalkboard sign.
function LogoPantry({ size = 28, color = '#2F4A3A', wordColor }) {
  return (
    <div style={{ display: 'inline-flex', alignItems: 'center', gap: 10, color }}>
      <svg width={size} height={size} viewBox="0 0 32 32" fill="none">
        {/* basket body — slightly tilted, tapered */}
        <path d="M5 10.5h22l-2.2 15a3 3 0 0 1-3 2.5H10.2a3 3 0 0 1-3-2.5l-2.2-15z"
          stroke={color} strokeWidth="2" strokeLinejoin="round" fill="none" />
        {/* weave lines */}
        <path d="M7 16h18M7.5 20.5h17M8 25h16" stroke={color} strokeWidth="1.25" strokeOpacity="0.4" strokeLinecap="round" />
        {/* handle */}
        <path d="M10 10.5c0-3.5 2.5-6 6-6s6 2.5 6 6" stroke={color} strokeWidth="2" strokeLinecap="round" fill="none" />
        {/* little tag */}
        <circle cx="23" cy="7.5" r="1.8" fill={color} />
      </svg>
      <span style={{
        fontFamily: '"Inter Tight", Helvetica, sans-serif',
        fontSize: size * 0.75, fontWeight: 650, letterSpacing: -0.3,
        color: wordColor || color,
      }}>Shoplists</span>
    </div>
  );
}

// Market mark — more abstract. A folded receipt with a leaf notch.
function LogoMarket({ size = 28, color = '#0F6B50', wordColor }) {
  return (
    <div style={{ display: 'inline-flex', alignItems: 'center', gap: 10, color }}>
      <svg width={size} height={size} viewBox="0 0 32 32" fill="none">
        {/* receipt / card */}
        <path d="M7 4h14a2 2 0 0 1 2 2v20l-3-2-3 2-3-2-3 2-3-2-3 2V6a2 2 0 0 1 2-2z"
          fill={color} />
        {/* tick rows */}
        <path d="M11 10h8M11 14h8M11 18h5" stroke="#fff" strokeWidth="1.5" strokeLinecap="round" opacity="0.9" />
        {/* leaf accent */}
        <path d="M22 4c3 0 5 2 5 5-2 0-4-1-5-3v-2z" fill={color} />
      </svg>
      <span style={{
        fontFamily: '"Inter Tight", Helvetica, sans-serif',
        fontSize: size * 0.75, fontWeight: 650, letterSpacing: -0.3,
        color: wordColor || '#111714',
      }}>Shoplists</span>
    </div>
  );
}

Object.assign(window, { LogoPantry, LogoMarket });
