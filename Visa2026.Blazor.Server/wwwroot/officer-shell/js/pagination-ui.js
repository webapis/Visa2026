/** Shared list/grid pagination — PNG parity (P6). */

export const PAGE_SIZE_OPTIONS = [10, 25, 50];

/**
 * @param {Array} items
 * @param {{ page: number, pageSize: number }} state
 */
export function paginateSlice(items, state) {
  const total = items.length;
  const pageSize = state.pageSize || PAGE_SIZE_OPTIONS[0];
  const totalPages = Math.max(1, Math.ceil(total / pageSize) || 1);
  const page = Math.min(Math.max(1, state.page || 1), totalPages);
  const startIndex = total === 0 ? 0 : (page - 1) * pageSize;
  const endIndex = Math.min(startIndex + pageSize, total);
  const pageItems = items.slice(startIndex, endIndex);
  return {
    pageItems,
    page,
    pageSize,
    total,
    totalPages,
    start: total === 0 ? 0 : startIndex + 1,
    end: endIndex,
  };
}

function pageSequence(current, totalPages) {
  if (totalPages <= 7) {
    return Array.from({ length: totalPages }, (_, i) => ({ type: 'page', num: i + 1 }));
  }
  const pages = [];
  const add = n => {
    if (n >= 1 && n <= totalPages && !pages.some(p => p.type === 'page' && p.num === n)) {
      pages.push({ type: 'page', num: n });
    }
  };
  add(1);
  if (current > 3) pages.push({ type: 'ellipsis' });
  for (let n = current - 1; n <= current + 1; n++) add(n);
  if (current < totalPages - 2) pages.push({ type: 'ellipsis' });
  add(totalPages);
  return pages;
}

/**
 * @param {string} pageId — store key: staged | inProcess | templates
 * @param {ReturnType<typeof paginateSlice>} meta
 */
export function renderPaginationBar(pageId, meta) {
  const { page, pageSize, total, totalPages, start, end } = meta;
  const sizeOptions = PAGE_SIZE_OPTIONS.map(n =>
    `<option value="${n}"${n === pageSize ? ' selected' : ''}>${n}</option>`).join('');

  const pageButtons = pageSequence(page, totalPages).map(item => {
    if (item.type === 'ellipsis') {
      return '<li class="page-item disabled"><span class="page-link">…</span></li>';
    }
    const active = item.num === page ? ' active' : '';
    const aria = item.num === page ? ' aria-current="page"' : '';
    return `<li class="page-item${active}">
      <button type="button" class="page-link" data-pager-page="${pageId}" data-page-num="${item.num}"${aria}>${item.num}</button>
    </li>`;
  }).join('');

  const prevDisabled = page <= 1 ? ' disabled' : '';
  const nextDisabled = page >= totalPages ? ' disabled' : '';
  const prevPage = page - 1;
  const nextPage = page + 1;

  const summary = total === 0
    ? 'Showing 0 of 0'
    : `Showing ${start}–${end} of ${total}`;

  return `<div class="os-pager" data-pager-id="${pageId}">
    <div class="os-pager__summary">${summary}</div>
    <div class="os-pager__size">
      <label class="os-pager__size-label" for="pager-size-${pageId}">Rows per page</label>
      <select class="os-pager__size-select form-select form-select-sm" id="pager-size-${pageId}"
        data-pager-size="${pageId}" aria-label="Rows per page">${sizeOptions}</select>
    </div>
    <nav class="os-pager__nav" aria-label="Pagination">
      <ul class="pagination pagination-sm mb-0">
        <li class="page-item${prevDisabled}">
          <button type="button" class="page-link" data-pager-page="${pageId}" data-page-num="${prevPage}"
            ${page <= 1 ? 'disabled' : ''} aria-label="Previous page"><i class="bi bi-chevron-left"></i></button>
        </li>
        ${pageButtons}
        <li class="page-item${nextDisabled}">
          <button type="button" class="page-link" data-pager-page="${pageId}" data-page-num="${nextPage}"
            ${page >= totalPages ? 'disabled' : ''} aria-label="Next page"><i class="bi bi-chevron-right"></i></button>
        </li>
      </ul>
    </nav>
  </div>`;
}
