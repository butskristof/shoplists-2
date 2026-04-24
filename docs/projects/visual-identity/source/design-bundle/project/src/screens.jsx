// App screens styled per direction. Each screen accepts a `t` (tokens).
// Screens: Home (lists), Detail (view), Edit, Empty

// ─────────────────────────────────────────────────────────────
// Shared atomic pieces that adapt to tokens
// ─────────────────────────────────────────────────────────────
function AppHeader({ t, logo }) {
  return (
    <div style={{
      padding: '56px 20px 16px', display: 'flex', alignItems: 'center',
      justifyContent: 'space-between', borderBottom: `1px solid ${t.rule}`,
      background: t.bg,
    }}>
      {logo}
      <div style={{ display: 'flex', gap: 10, color: t.inkSoft }}>
        <IconSparkle w={20} />
      </div>
    </div>
  );
}

function Btn({ t, children, icon, kind = 'primary', size = 'md', style }) {
  const pads = size === 'sm' ? '8px 12px' : '11px 16px';
  const fs = size === 'sm' ? 13 : 15;
  const styles = {
    primary: { background: t.primary, color: t.primaryInk, border: `1px solid ${t.primary}` },
    ghost:   { background: 'transparent', color: t.primary, border: `1px solid ${t.rule}` },
    danger:  { background: 'transparent', color: t.berry, border: `1px solid ${t.rule}` },
    subtle:  { background: t.surfaceAlt, color: t.ink, border: `1px solid transparent` },
  }[kind];
  return (
    <button style={{
      display: 'inline-flex', alignItems: 'center', gap: 6,
      padding: pads, borderRadius: t.r, ...styles,
      fontFamily: t.font, fontSize: fs, fontWeight: 550,
      letterSpacing: -0.1, cursor: 'pointer', lineHeight: 1,
      ...style,
    }}>
      {icon}{children}
    </button>
  );
}

// Checkbox — custom per direction feel (square, confident)
function Check({ t, checked, size = 22 }) {
  return (
    <div style={{
      width: size, height: size, borderRadius: 5,
      border: `1.5px solid ${checked ? t.primary : t.rule}`,
      background: checked ? t.primary : t.surface,
      display: 'flex', alignItems: 'center', justifyContent: 'center',
      flexShrink: 0, transition: 'all 150ms',
      color: t.primaryInk,
    }}>
      {checked && <IconCheck w={size * 0.7} />}
    </div>
  );
}

// ─────────────────────────────────────────────────────────────
// HOME — Your lists
// ─────────────────────────────────────────────────────────────
function ScreenHome({ t, flavor, logo, lists }) {
  return (
    <div style={{ background: t.bg, minHeight: '100%', fontFamily: t.font, color: t.ink }}>
      <AppHeader t={t} logo={logo} />
      <div style={{ padding: '24px 20px 12px' }}>
        <div style={{
          fontFamily: t.mono, fontSize: 11, letterSpacing: 1.6, fontWeight: 600,
          color: t.muted, textTransform: 'uppercase',
          marginBottom: 6,
        }}>{flavor === 'pantry' ? 'The Pantry' : 'Your Week'}</div>
        <div style={{
          display: 'flex', justifyContent: 'space-between', alignItems: 'flex-end',
        }}>
          <h1 style={{
            margin: 0, fontSize: 32, fontWeight: 700, letterSpacing: -0.8,
            lineHeight: 1.05,
          }}>Your lists</h1>
          <Btn t={t} kind="primary" size="sm" icon={<IconPlus w={14} />}>New list</Btn>
        </div>
      </div>

      <div style={{ padding: '8px 20px 20px', display: 'flex', flexDirection: 'column', gap: 10 }}>
        {lists.map((l, i) => (
          <ListCard key={i} t={t} flavor={flavor} {...l} />
        ))}
      </div>
    </div>
  );
}

function ListCard({ t, flavor, title, total, done, hint }) {
  const pct = total === 0 ? 0 : done / total;
  return (
    <div style={{
      background: t.surface, borderRadius: t.rLg,
      border: `1px solid ${t.rule}`,
      padding: '16px 18px',
      display: 'flex', alignItems: 'center', gap: 14,
      boxShadow: t.shadow,
    }}>
      {/* progress ring */}
      <div style={{
        width: 44, height: 44, borderRadius: 22,
        background: `conic-gradient(${t.primary} ${pct * 360}deg, ${t.surfaceAlt} 0deg)`,
        display: 'flex', alignItems: 'center', justifyContent: 'center',
        flexShrink: 0,
      }}>
        <div style={{
          width: 34, height: 34, borderRadius: 17, background: t.surface,
          display: 'flex', alignItems: 'center', justifyContent: 'center',
          fontFamily: t.font, fontSize: 12, fontWeight: 600, color: t.ink,
          letterSpacing: -0.1,
        }}>{done}/{total}</div>
      </div>
      <div style={{ flex: 1 }}>
        <div style={{ fontSize: 17, fontWeight: 600, letterSpacing: -0.2 }}>{title}</div>
        <div style={{ fontSize: 13, color: t.muted, marginTop: 3, letterSpacing: -0.1 }}>{hint}</div>
      </div>
      <div style={{ color: t.inkSoft }}>
        <IconChevron w={18} />
      </div>
    </div>
  );
}

// ─────────────────────────────────────────────────────────────
// DETAIL — Items to get / Fulfilled
// ─────────────────────────────────────────────────────────────
function ScreenDetail({ t, flavor, logo, listName, todo, done }) {
  const total = todo.length + done.length;
  const pct = total === 0 ? 0 : done.length / total;
  return (
    <div style={{ background: t.bg, minHeight: '100%', fontFamily: t.font, color: t.ink }}>
      <AppHeader t={t} logo={logo} />

      <div style={{ padding: '18px 20px 8px' }}>
        <button style={{
          display: 'inline-flex', alignItems: 'center', gap: 6,
          padding: 0, border: 0, background: 'transparent',
          color: t.primary, fontFamily: t.font, fontSize: 14, fontWeight: 550,
          cursor: 'pointer',
        }}>
          <IconArrowLeft w={16} /> Back to lists
        </button>
      </div>

      <div style={{ padding: '6px 20px 12px' }}>
        <h1 style={{
          margin: 0, fontSize: 28, fontWeight: 700, letterSpacing: -0.6,
          lineHeight: 1.1,
        }}>{listName}</h1>
        <div style={{
          marginTop: 10, display: 'flex', alignItems: 'center', gap: 10,
        }}>
          <div style={{
            flex: 1, height: 6, background: t.surfaceAlt, borderRadius: 3,
            overflow: 'hidden',
          }}>
            <div style={{
              width: `${pct * 100}%`, height: '100%',
              background: t.primary, borderRadius: 3, transition: 'width 300ms',
            }} />
          </div>
          <div style={{
            fontFamily: t.font, fontSize: 13, fontWeight: 600, color: t.inkSoft,
            letterSpacing: -0.1,
          }}>{done.length}/{total}</div>
          <Btn t={t} kind="ghost" size="sm" icon={<IconPencil w={13} />}>Edit</Btn>
        </div>
      </div>

      {/* Items to get */}
      <SectionLabel t={t}>Items to get</SectionLabel>
      <div style={{ padding: '0 12px', marginBottom: 20 }}>
        <div style={{
          background: t.surface, borderRadius: t.rLg,
          border: `1px solid ${t.rule}`, overflow: 'hidden',
        }}>
          {todo.map((name, i) => (
            <ItemRow key={i} t={t} name={name} last={i === todo.length - 1} />
          ))}
        </div>
      </div>

      {/* Fulfilled */}
      <SectionLabel t={t}>Fulfilled</SectionLabel>
      <div style={{ padding: '0 12px 24px' }}>
        <div style={{
          background: t.surface, borderRadius: t.rLg,
          border: `1px solid ${t.rule}`, overflow: 'hidden', opacity: 0.85,
        }}>
          {done.map((name, i) => (
            <ItemRow key={i} t={t} name={name} checked last={i === done.length - 1} />
          ))}
        </div>
      </div>
    </div>
  );
}

function SectionLabel({ t, children }) {
  return (
    <div style={{
      padding: '6px 28px 8px',
      fontFamily: t.mono, fontSize: 11, letterSpacing: 1.6,
      fontWeight: 600, color: t.muted, textTransform: 'uppercase',
    }}>{children}</div>
  );
}

function ItemRow({ t, name, checked, last }) {
  return (
    <div style={{
      display: 'flex', alignItems: 'center', gap: 14,
      padding: '14px 16px',
      borderBottom: last ? 'none' : `1px solid ${t.rule}`,
    }}>
      <Check t={t} checked={checked} />
      <div style={{
        flex: 1, fontSize: 16, lineHeight: 1.35,
        color: checked ? t.muted : t.ink,
        textDecoration: checked ? 'line-through' : 'none',
        textDecorationColor: t.muted,
      }}>{name}</div>
    </div>
  );
}

// ─────────────────────────────────────────────────────────────
// EDIT mode
// ─────────────────────────────────────────────────────────────
function ScreenEdit({ t, flavor, logo, listName, items }) {
  return (
    <div style={{ background: t.bg, minHeight: '100%', fontFamily: t.font, color: t.ink }}>
      <AppHeader t={t} logo={logo} />
      <div style={{ padding: '18px 20px 6px' }}>
        <button style={{
          display: 'inline-flex', alignItems: 'center', gap: 6,
          padding: 0, border: 0, background: 'transparent',
          color: t.primary, fontFamily: t.font, fontSize: 14, fontWeight: 550,
          cursor: 'pointer',
        }}><IconArrowLeft w={16} /> Back to lists</button>
      </div>
      <div style={{
        padding: '6px 20px 14px', display: 'flex',
        justifyContent: 'space-between', alignItems: 'center',
      }}>
        <h1 style={{ margin: 0, fontSize: 26, fontWeight: 700, letterSpacing: -0.5 }}>
          {listName}
        </h1>
        <Btn t={t} kind="primary" size="sm" icon={<IconX w={13} />}>Done</Btn>
      </div>

      <div style={{ padding: '0 12px 16px' }}>
        <div style={{
          background: t.surface, borderRadius: t.rLg,
          border: `1px solid ${t.rule}`, overflow: 'hidden',
        }}>
          {items.map((it, i) => (
            <EditRow key={i} t={t} name={it} last={i === items.length - 1} editing={i === 0} />
          ))}
        </div>
      </div>

      {/* add new */}
      <div style={{ padding: '0 20px 12px', display: 'flex', gap: 10, alignItems: 'center' }}>
        <div style={{
          flex: 1, padding: '11px 14px',
          background: t.surface, border: `1px dashed ${t.rule}`, borderRadius: t.r,
          color: t.muted, fontSize: 15, fontFamily: t.font,
        }}>Add a new item…</div>
        <button style={{
          width: 40, height: 40, borderRadius: t.r,
          background: t.primary, color: t.primaryInk,
          border: 'none', cursor: 'pointer',
          display: 'flex', alignItems: 'center', justifyContent: 'center',
        }}><IconPlus w={18} /></button>
      </div>

      <div style={{ padding: '16px 20px 30px' }}>
        <Btn t={t} kind="danger" size="sm" icon={<IconTrash w={13} />}>Delete list</Btn>
      </div>
    </div>
  );
}

function EditRow({ t, name, last, editing }) {
  return (
    <div style={{
      display: 'flex', alignItems: 'center', gap: 10,
      padding: '10px 10px 10px 6px',
      borderBottom: last ? 'none' : `1px solid ${t.rule}`,
      background: editing ? t.surfaceAlt : 'transparent',
    }}>
      <div style={{ color: t.muted, padding: '0 4px', display: 'flex' }}>
        <IconGrip w={16} />
      </div>
      {editing ? (
        <div style={{
          flex: 1, padding: '8px 12px', background: t.surface,
          borderRadius: t.r, border: `1.5px solid ${t.primary}`,
          fontSize: 15, color: t.ink,
        }}>{name}<span style={{
          display: 'inline-block', width: 1, height: 16,
          background: t.primary, marginLeft: 2, verticalAlign: 'middle',
        }}/></div>
      ) : (
        <div style={{ flex: 1, fontSize: 15 }}>{name}</div>
      )}
      <div style={{ display: 'flex', gap: 6 }}>
        {editing ? (
          <>
            <button style={iconBtn(t, t.primary)}><IconCheck w={16} /></button>
            <button style={iconBtn(t, t.muted)}><IconX w={16} /></button>
          </>
        ) : (
          <>
            <button style={iconBtn(t, t.primary)}><IconPencil w={16} /></button>
            <button style={iconBtn(t, t.berry)}><IconTrash w={16} /></button>
          </>
        )}
      </div>
    </div>
  );
}

function iconBtn(t, color) {
  return {
    width: 32, height: 32, borderRadius: t.r,
    border: 'none', background: 'transparent', color,
    cursor: 'pointer', display: 'flex',
    alignItems: 'center', justifyContent: 'center',
  };
}

Object.assign(window, {
  ScreenHome, ScreenDetail, ScreenEdit,
  Btn, Check, AppHeader,
});
