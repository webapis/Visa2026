/** Sidebar nav badges + ApplicationProfileInstance profiles nav items — PNG parity (P9). */

/**
 * @param {ReturnType<typeof import('./mock-data.js').getStore>} store
 */
export function getApplicationProfileNavItems(store) {
  return [
    {
      path: '#/staged',
      icon: '📚',
      title: 'Staged profiles',
      sub: 'Before application number',
      badge: 'orange',
      count: store.staged.length,
      badgeLabel: `${store.staged.length} staged profiles`,
    },
    {
      path: '#/in-process',
      icon: '📁',
      title: 'In process',
      sub: 'Numbered cases',
      badge: 'blue',
      count: store.inProcess.length,
      badgeLabel: `${store.inProcess.length} in-process cases`,
    },
    {
      path: '#/templates',
      icon: '📋',
      title: 'Profile templates',
      sub: 'Configuration · Visa office admin',
    },
  ];
}

/** @param {'orange'|'blue'} variant */
export function renderNavBadge(count, variant, label) {
  if (count == null) return '';
  return `<span class="os-nav-badge os-nav-badge--${variant}" aria-label="${label ?? `${count} items`}">${count}</span>`;
}

export const NAV_DEMO_TARGETS = { staged: 18, inProcess: 24 };
