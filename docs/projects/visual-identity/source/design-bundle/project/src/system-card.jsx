// Design system display card — renders the system for one direction.

function SystemCard({ t, flavor, logoLarge }) {
  const swatches = [
    ['Background', t.bg],
    ['Surface', t.surface],
    ['Surface Alt', t.surfaceAlt],
    ['Ink', t.ink],
    ['Ink Soft', t.inkSoft],
    ['Muted', t.muted],
    ['Primary', t.primary],
    ['Accent', t.accent],
    ['Berry', t.berry],
  ];

  return (
    <div style={{
      width: 920, background: t.bg, padding: 44,
      fontFamily: t.font, color: t.ink, border: `1px solid ${t.rule}`,
      borderRadius: 4,
    }}>
      {/* Header — brand */}
      <div style={{
        display: 'flex', justifyContent: 'space-between', alignItems: 'flex-start',
        paddingBottom: 32, borderBottom: `1px solid ${t.rule}`,
      }}>
        <div>
          <div style={{
            fontFamily: t.mono, fontSize: 11, letterSpacing: 1.8,
            textTransform: 'uppercase', color: t.muted, fontWeight: 600,
          }}>Direction · {flavor === 'pantry' ? 'A' : 'B'}</div>
          <div style={{
            fontSize: 52, fontWeight: 700, letterSpacing: -1.5,
            marginTop: 6, lineHeight: 1,
          }}>{t.name}</div>
          <div style={{
            fontSize: 16, color: t.inkSoft, marginTop: 12, maxWidth: 440,
            lineHeight: 1.4,
          }}>{t.tagline}</div>
        </div>
        <div style={{ paddingTop: 6 }}>{logoLarge}</div>
      </div>

      {/* Grid */}
      <div style={{
        display: 'grid', gridTemplateColumns: '1fr 1fr', gap: 32,
        marginTop: 32,
      }}>
        {/* Type */}
        <div>
          <Label t={t}>Typography</Label>
          <div style={{
            background: t.surface, border: `1px solid ${t.rule}`, borderRadius: t.rLg,
            padding: '24px 22px',
          }}>
            <div style={{ fontSize: 44, fontWeight: 700, letterSpacing: -1, lineHeight: 1 }}>
              Your lists
            </div>
            <div style={{ fontSize: 11, color: t.muted, fontFamily: t.mono, marginTop: 4 }}>
              H1 · Inter Tight 700 · 44/48 · -1
            </div>
            <div style={{ height: 1, background: t.rule, margin: '18px 0' }} />
            <div style={{ fontSize: 20, fontWeight: 600, letterSpacing: -0.3 }}>
              Weekly groceries
            </div>
            <div style={{ fontSize: 11, color: t.muted, fontFamily: t.mono, marginTop: 3 }}>
              H2 · Inter Tight 600 · 20/26
            </div>
            <div style={{ height: 1, background: t.rule, margin: '16px 0' }} />
            <div style={{ fontSize: 15, color: t.ink, lineHeight: 1.5 }}>
              Body copy for ingredients, item names, and descriptions.
            </div>
            <div style={{ fontSize: 11, color: t.muted, fontFamily: t.mono, marginTop: 3 }}>
              Body · Inter Tight 450 · 15/22
            </div>
            <div style={{ height: 1, background: t.rule, margin: '16px 0' }} />
            <div style={{
              fontFamily: t.mono, fontSize: 11, letterSpacing: 1.6,
              textTransform: 'uppercase', fontWeight: 600, color: t.muted,
            }}>Items to get</div>
            <div style={{ fontSize: 11, color: t.muted, fontFamily: t.mono, marginTop: 3 }}>
              Eyebrow · Inter Tight 600 · 11/14 · +1.6 · caps
            </div>
          </div>
        </div>

        {/* Colors */}
        <div>
          <Label t={t}>Palette</Label>
          <div style={{
            background: t.surface, border: `1px solid ${t.rule}`, borderRadius: t.rLg,
            padding: 18,
          }}>
            <div style={{ display: 'grid', gridTemplateColumns: 'repeat(3, 1fr)', gap: 10 }}>
              {swatches.map(([name, hex]) => (
                <div key={name} style={{
                  border: `1px solid ${t.rule}`, borderRadius: t.r, overflow: 'hidden',
                }}>
                  <div style={{ height: 52, background: hex }} />
                  <div style={{ padding: '8px 10px' }}>
                    <div style={{ fontSize: 12, fontWeight: 600 }}>{name}</div>
                    <div style={{
                      fontSize: 10, color: t.muted, fontFamily: t.mono, marginTop: 2,
                    }}>{hex.toUpperCase()}</div>
                  </div>
                </div>
              ))}
            </div>
          </div>
        </div>

        {/* Components */}
        <div style={{ gridColumn: '1 / -1' }}>
          <Label t={t}>Components</Label>
          <div style={{
            background: t.surface, border: `1px solid ${t.rule}`, borderRadius: t.rLg,
            padding: '22px 24px',
            display: 'grid', gridTemplateColumns: '1fr 1fr', gap: 32,
          }}>
            {/* Buttons */}
            <div>
              <Sub t={t}>Buttons</Sub>
              <div style={{ display: 'flex', gap: 10, flexWrap: 'wrap' }}>
                <Btn t={t} kind="primary" icon={<IconPlus w={14} />}>New list</Btn>
                <Btn t={t} kind="ghost" icon={<IconPencil w={14} />}>Edit</Btn>
                <Btn t={t} kind="subtle">Cancel</Btn>
                <Btn t={t} kind="danger" icon={<IconTrash w={14} />}>Delete</Btn>
              </div>
            </div>
            {/* Checkbox states */}
            <div>
              <Sub t={t}>Item states</Sub>
              <div style={{ display: 'flex', gap: 18, alignItems: 'center' }}>
                <div style={{ display: 'flex', gap: 10, alignItems: 'center' }}>
                  <Check t={t} /> <span style={{ fontSize: 14 }}>To get</span>
                </div>
                <div style={{ display: 'flex', gap: 10, alignItems: 'center' }}>
                  <Check t={t} checked /> <span style={{
                    fontSize: 14, color: t.muted, textDecoration: 'line-through',
                  }}>Fulfilled</span>
                </div>
              </div>
            </div>
            {/* List row */}
            <div style={{ gridColumn: '1 / -1' }}>
              <Sub t={t}>List row</Sub>
              <div style={{
                border: `1px solid ${t.rule}`, borderRadius: t.rLg, overflow: 'hidden',
              }}>
                <div style={{
                  display: 'flex', alignItems: 'center', gap: 14,
                  padding: '14px 16px', borderBottom: `1px solid ${t.rule}`,
                }}>
                  <Check t={t} />
                  <div style={{ flex: 1, fontSize: 15 }}>Peanut butter</div>
                </div>
                <div style={{
                  display: 'flex', alignItems: 'center', gap: 14, padding: '14px 16px',
                }}>
                  <Check t={t} checked />
                  <div style={{
                    flex: 1, fontSize: 15, color: t.muted, textDecoration: 'line-through',
                  }}>Milk</div>
                </div>
              </div>
            </div>
          </div>
        </div>

        {/* Tokens */}
        <div style={{ gridColumn: '1 / -1' }}>
          <Label t={t}>Shape & motion tokens</Label>
          <div style={{
            background: t.surface, border: `1px solid ${t.rule}`, borderRadius: t.rLg,
            padding: '18px 22px', display: 'flex', gap: 40, flexWrap: 'wrap',
            fontFamily: t.mono, fontSize: 12, color: t.inkSoft,
          }}>
            <TokenBit label="radius-sm" val={`${t.r}px`}>
              <div style={{ width: 40, height: 40, background: t.primary, borderRadius: t.r }}/>
            </TokenBit>
            <TokenBit label="radius-lg" val={`${t.rLg}px`}>
              <div style={{ width: 40, height: 40, background: t.primary, borderRadius: t.rLg }}/>
            </TokenBit>
            <TokenBit label="shadow" val="soft, -y">
              <div style={{
                width: 40, height: 40, background: t.surface, borderRadius: t.rLg,
                boxShadow: t.shadow, border: `1px solid ${t.rule}`,
              }}/>
            </TokenBit>
            <TokenBit label="accent" val="confirm">
              <div style={{ width: 40, height: 40, background: t.accent, borderRadius: t.r }}/>
            </TokenBit>
            <TokenBit label="motion" val="120-220ms · ease-out"><div/></TokenBit>
          </div>
        </div>
      </div>
    </div>
  );
}

function Label({ t, children }) {
  return (
    <div style={{
      fontFamily: t.mono, fontSize: 11, letterSpacing: 1.6,
      textTransform: 'uppercase', color: t.muted, fontWeight: 600,
      marginBottom: 10,
    }}>{children}</div>
  );
}
function Sub({ t, children }) {
  return (
    <div style={{
      fontSize: 13, fontWeight: 600, color: t.inkSoft, marginBottom: 10,
      letterSpacing: -0.1,
    }}>{children}</div>
  );
}
function TokenBit({ label, val, children }) {
  return (
    <div style={{ display: 'flex', gap: 12, alignItems: 'center' }}>
      {children}
      <div>
        <div style={{ fontWeight: 600, color: 'inherit' }}>{label}</div>
        <div style={{ opacity: 0.7 }}>{val}</div>
      </div>
    </div>
  );
}

Object.assign(window, { SystemCard });
