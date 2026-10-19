window.MajestyBoard = (() => {
  const NAME = { k: 'King', q: 'Queen', r: 'Rook', b: 'Bishop', n: 'Knight', p: 'Pawn' };

  function pieceSrc(ch) {
    const lower = ch.toLowerCase();
    const color = ch === lower ? 'Black' : 'White';
    return `/pieces/Light/${NAME[lower]}_${color}.png`;
  }

  function fenToMap(fen) {
    const map = {};
    const rows = fen.split(' ')[0].split('/');
    for (let r = 0; r < 8; r++) {
      let f = 0;
      for (const c of rows[r]) {
        if (/\d/.test(c)) f += +c;
        else {
          map[String.fromCharCode(97 + f) + (8 - r)] = c;
          f++;
        }
      }
    }
    return map;
  }

  function mapToFen(map, side) {
    const rows = [];
    for (let r = 0; r < 8; r++) {
      let empty = 0, row = '';
      for (let f = 0; f < 8; f++) {
        const p = map[String.fromCharCode(97 + f) + (8 - r)];
        if (!p) empty++;
        else {
          if (empty) { row += empty; empty = 0; }
          row += p;
        }
      }
      if (empty) row += empty;
      rows.push(row);
    }
    return `${rows.join('/')} ${side} - - 0 1`;
  }

  /** flip=true: xoay bàn cho bên đen (giống FE cũ) */
  function render(el, fen, selected, flip) {
    const map = fenToMap(fen);
    el.innerHTML = '';
    for (let vr = 0; vr < 8; vr++) {
      for (let vf = 0; vf < 8; vf++) {
        const r = flip ? 7 - vr : vr;
        const f = flip ? 7 - vf : vf;
        const sq = String.fromCharCode(97 + f) + (8 - r);
        const cell = document.createElement('div');
        cell.className = 'sq ' + (((r + f) % 2 === 1) ? 'dark' : 'light');
        cell.dataset.sq = sq;
        if (selected === sq) cell.classList.add('selected');
        const p = map[sq];
        if (p) {
          const img = document.createElement('img');
          img.src = pieceSrc(p);
          img.alt = p;
          cell.appendChild(img);
        }
        el.appendChild(cell);
      }
    }
  }

  return { fenToMap, mapToFen, render, pieceSrc };
})();
