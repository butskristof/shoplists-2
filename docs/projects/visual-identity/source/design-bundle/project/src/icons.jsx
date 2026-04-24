// Custom Shoplists iconography — drawn fresh, not Primevue defaults.
// All icons: 24x24 viewBox, stroke-based, currentColor.

const S = ({ w = 20, children, style }) => (
  <svg width={w} height={w} viewBox="0 0 24 24" fill="none"
    stroke="currentColor" strokeWidth="1.75" strokeLinecap="round" strokeLinejoin="round"
    style={style}>
    {children}
  </svg>
);

// Redesigned cart — simpler, more graphic. A basket with a handle.
const IconBasket = ({ w }) => (
  <S w={w}>
    <path d="M4 7h16l-1.5 10.5a2 2 0 0 1-2 1.75h-9a2 2 0 0 1-2-1.75L4 7z" />
    <path d="M8 7V5a4 4 0 0 1 8 0v2" />
    <path d="M9 11v5M15 11v5M12 11v5" opacity="0.45" />
  </S>
);

// Check — rounded, confident
const IconCheck = ({ w }) => (
  <S w={w}><path d="M4.5 12.5l4.5 4.5 10.5-11" /></S>
);

// Plus
const IconPlus = ({ w }) => (
  <S w={w}><path d="M12 5v14M5 12h14" /></S>
);

// Pencil
const IconPencil = ({ w }) => (
  <S w={w}>
    <path d="M4 20h4l11-11-4-4L4 16v4z" />
    <path d="M14 6l4 4" />
  </S>
);

// Trash
const IconTrash = ({ w }) => (
  <S w={w}>
    <path d="M4 7h16" />
    <path d="M10 4h4a1 1 0 0 1 1 1v2H9V5a1 1 0 0 1 1-1z" />
    <path d="M6 7l1 12a2 2 0 0 0 2 1.8h6a2 2 0 0 0 2-1.8l1-12" />
  </S>
);

// Drag handle — 6 dots
const IconGrip = ({ w }) => (
  <S w={w}>
    <circle cx="9" cy="6" r="0.9" fill="currentColor" />
    <circle cx="15" cy="6" r="0.9" fill="currentColor" />
    <circle cx="9" cy="12" r="0.9" fill="currentColor" />
    <circle cx="15" cy="12" r="0.9" fill="currentColor" />
    <circle cx="9" cy="18" r="0.9" fill="currentColor" />
    <circle cx="15" cy="18" r="0.9" fill="currentColor" />
  </S>
);

// Chevron
const IconChevron = ({ w, dir = 'right' }) => {
  const rot = { right: 0, left: 180, up: -90, down: 90 }[dir];
  return (
    <S w={w} style={{ transform: `rotate(${rot}deg)` }}>
      <path d="M9 5l7 7-7 7" />
    </S>
  );
};

// Close
const IconX = ({ w }) => (
  <S w={w}><path d="M6 6l12 12M18 6L6 18" /></S>
);

// Arrow left
const IconArrowLeft = ({ w }) => (
  <S w={w}><path d="M19 12H5M11 6l-6 6 6 6" /></S>
);

// Sparkle (for empty states)
const IconSparkle = ({ w }) => (
  <S w={w}>
    <path d="M12 3v6M12 15v6M3 12h6M15 12h6" />
    <path d="M6.5 6.5l3 3M14.5 14.5l3 3M17.5 6.5l-3 3M9.5 14.5l-3 3" opacity="0.5" />
  </S>
);

// Leaf — for Market brand mark
const IconLeaf = ({ w }) => (
  <S w={w}>
    <path d="M5 19c0-8 6-14 15-14 0 9-6 15-14 15-0.5 0-1 0-1-1z" />
    <path d="M5 19c4-4 8-6 13-7" />
  </S>
);

Object.assign(window, {
  IconBasket, IconCheck, IconPlus, IconPencil, IconTrash, IconGrip,
  IconChevron, IconX, IconArrowLeft, IconSparkle, IconLeaf,
});
