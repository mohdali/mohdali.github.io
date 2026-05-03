import { chromium } from 'playwright';
import YAML from 'yaml';
import { createServer } from 'http';
import { createReadStream, existsSync, readFileSync, writeFileSync, mkdirSync, readdirSync, statSync } from 'fs';
import { join, dirname, extname, basename } from 'path';
import { fileURLToPath } from 'url';

const __dirname = dirname(fileURLToPath(import.meta.url));
const repoRoot = join(__dirname, '..');
const wwwroot = join(__dirname, 'output', 'wwwroot');
const postsDir = join(repoRoot, 'src', 'mohdali.github.io', 'Pages', 'Posts');
const siteUrl = 'https://mohdali.dev';
const preferredPort = Number(process.env.PRERENDER_PORT ?? 0);
const publicationDate = new Date().toISOString().slice(0, 10);

const mimeTypes = {
  '.html': 'text/html',
  '.js': 'application/javascript',
  '.css': 'text/css',
  '.json': 'application/json',
  '.wasm': 'application/wasm',
  '.svg': 'image/svg+xml',
  '.jpg': 'image/jpeg',
  '.jpeg': 'image/jpeg',
  '.png': 'image/png',
  '.ico': 'image/x-icon',
  '.xml': 'application/xml',
  '.txt': 'text/plain',
  '.woff': 'font/woff',
  '.woff2': 'font/woff2',
  '.ttf': 'font/ttf',
  '.eot': 'application/vnd.ms-fontobject'
};

const server = createServer((req, res) => {
  const requestPath = decodeURIComponent((req.url ?? '/').split('?')[0]);
  const hasExtension = extname(requestPath) !== '';
  let filePath = join(wwwroot, requestPath === '/' || !hasExtension ? 'index.html' : requestPath);

  if (existsSync(filePath) && statSync(filePath).isDirectory()) {
    filePath = join(filePath, 'index.html');
  }

  if (existsSync(filePath + '.gz')) {
    filePath = filePath + '.gz';
    res.setHeader('Content-Encoding', 'gzip');
  } else if (existsSync(filePath + '.br')) {
    filePath = filePath + '.br';
    res.setHeader('Content-Encoding', 'br');
  } else if (!existsSync(filePath)) {
    filePath = join(wwwroot, 'index.html');
    if (existsSync(filePath + '.gz')) {
      filePath = filePath + '.gz';
      res.setHeader('Content-Encoding', 'gzip');
    }
  }

  if (existsSync(filePath)) {
    const ext = extname(filePath.replace(/\.(gz|br)$/, ''));
    res.setHeader('Content-Type', mimeTypes[ext] || 'application/octet-stream');
    createReadStream(filePath).pipe(res);
  } else {
    res.statusCode = 404;
    res.end('Not found');
  }
});

function findFiles(dir, predicate) {
  const files = [];

  for (const item of readdirSync(dir)) {
    const fullPath = join(dir, item);
    const stat = statSync(fullPath);

    if (stat.isDirectory()) {
      files.push(...findFiles(fullPath, predicate));
    } else if (predicate(item, fullPath)) {
      files.push(fullPath);
    }
  }

  return files;
}

function parseFileName(filePath) {
  const fileName = basename(filePath, extname(filePath));
  const match = fileName.match(/^(\d{4})-(\d{2})-(\d{2})-(.+)$/);

  if (!match) {
    return {
      date: '',
      slug: slugify(fileName),
      title: titleize(fileName)
    };
  }

  return {
    date: `${match[1]}-${match[2]}-${match[3]}`,
    slug: slugify(match[4]),
    title: titleize(match[4])
  };
}

function slugify(value) {
  const slug = String(value)
    .trim()
    .toLowerCase()
    .replace(/[^a-z0-9]+/g, '-')
    .replace(/^-+|-+$/g, '');

  return slug || 'post';
}

function titleize(value) {
  return String(value)
    .replace(/[-_]+/g, ' ')
    .replace(/\s+/g, ' ')
    .trim()
    .replace(/\b\w/g, letter => letter.toUpperCase());
}

function normalizeDate(value, fallback = '') {
  if (!value) {
    return fallback;
  }

  if (value instanceof Date) {
    return value.toISOString().slice(0, 10);
  }

  const text = String(value).trim();
  const match = text.match(/^\d{4}-\d{2}-\d{2}/);
  return match ? match[0] : fallback;
}

function normalizeTags(value) {
  if (Array.isArray(value)) {
    return value.map(tag => String(tag).trim()).filter(Boolean);
  }

  if (typeof value === 'string') {
    return value.split(',').map(tag => tag.trim()).filter(Boolean);
  }

  return [];
}

function isPublished(post) {
  return !post.date || post.date <= publicationDate;
}

function comparePosts(a, b) {
  return (b.date || '').localeCompare(a.date || '') ||
    (a.title || '').localeCompare(b.title || '') ||
    (a.route || '').localeCompare(b.route || '');
}

function extractRazorStringProperty(content, propertyName) {
  const pattern = new RegExp(`public\\s+string\\s+${propertyName}\\s*\\{[^}]*\\}\\s*=\\s*\"([^\"]*)\"`, 'm');
  return content.match(pattern)?.[1] ?? '';
}

function extractRazorTags(content) {
  const match = content.match(/public\s+string\[\]\s+Tags\s*\{[^}]*\}\s*=\s*\[([^\]]*)\]/m);

  if (!match) {
    return [];
  }

  return [...match[1].matchAll(/"([^"]+)"/g)].map(tag => tag[1]);
}

function extractFrontmatter(content) {
  const match = content.match(/^---\r?\n([\s\S]*?)\r?\n---/);
  return match ? YAML.parse(match[1]) ?? {} : {};
}

function discoverRazorPosts() {
  return findFiles(postsDir, item => item.endsWith('.razor') && !item.startsWith('_'))
    .map(filePath => {
      const content = readFileSync(filePath, 'utf8');
      const pageMatch = content.match(/@page\s+"([^"]+)"/);

      if (!pageMatch) {
        return null;
      }

      const fileMeta = parseFileName(filePath);
      return {
        route: pageMatch[1],
        title: fileMeta.title,
        date: fileMeta.date,
        description: extractRazorStringProperty(content, 'Description'),
        tags: extractRazorTags(content)
      };
    })
    .filter(Boolean);
}

function discoverMarkdownPosts() {
  return findFiles(postsDir, item => item.endsWith('.md') && !item.startsWith('_'))
    .map(filePath => {
      const content = readFileSync(filePath, 'utf8');
      const frontmatter = extractFrontmatter(content);

      if (frontmatter.draft === true || String(frontmatter.draft).toLowerCase() === 'true') {
        return null;
      }

      const fileMeta = parseFileName(filePath);
      const slug = frontmatter.slug ? slugify(frontmatter.slug) : fileMeta.slug;

      return {
        route: frontmatter.page || `/posts/${slug}`,
        title: frontmatter.title || fileMeta.title,
        date: normalizeDate(frontmatter.date, fileMeta.date),
        description: frontmatter.description || frontmatter.excerpt || frontmatter.summary || '',
        tags: normalizeTags(frontmatter.tags)
      };
    })
    .filter(Boolean);
}

function discoverContent() {
  const posts = [...discoverRazorPosts(), ...discoverMarkdownPosts()]
    .filter(isPublished)
    .sort(comparePosts);

  const routes = Array.from(new Set(['/', '/posts', '/about', ...posts.map(post => post.route)]));
  return { posts, routes };
}

function xmlEscape(value) {
  return String(value ?? '')
    .replace(/&/g, '&amp;')
    .replace(/</g, '&lt;')
    .replace(/>/g, '&gt;')
    .replace(/"/g, '&quot;')
    .replace(/'/g, '&apos;');
}

function absoluteUrl(route) {
  return `${siteUrl}${route === '/' ? '/' : route}`;
}

function writeDiscoveryFiles(posts, routes) {
  const rssItems = posts.map(post => `
    <item>
      <title>${xmlEscape(post.title)}</title>
      <link>${xmlEscape(absoluteUrl(post.route))}</link>
      <guid>${xmlEscape(absoluteUrl(post.route))}</guid>
      <pubDate>${new Date(`${post.date || publicationDate}T00:00:00Z`).toUTCString()}</pubDate>
      <description>${xmlEscape(post.description)}</description>
    </item>`).join('');

  writeFileSync(join(wwwroot, 'rss.xml'), `<?xml version="1.0" encoding="UTF-8"?>
<rss version="2.0">
  <channel>
    <title>mohdali.dev</title>
    <link>${siteUrl}/</link>
    <description>Software notes by Mohamed Ali.</description>
    <language>en</language>${rssItems}
  </channel>
</rss>
`);

  const sitemapUrls = routes.map(route => {
    const post = posts.find(candidate => candidate.route === route);
    const lastmod = post?.date || publicationDate;

    return `
  <url>
    <loc>${xmlEscape(absoluteUrl(route))}</loc>
    <lastmod>${lastmod}</lastmod>
  </url>`;
  }).join('');

  writeFileSync(join(wwwroot, 'sitemap.xml'), `<?xml version="1.0" encoding="UTF-8"?>
<urlset xmlns="http://www.sitemaps.org/schemas/sitemap/0.9">${sitemapUrls}
</urlset>
`);

  writeFileSync(join(wwwroot, 'robots.txt'), `User-agent: *
Allow: /
Sitemap: ${siteUrl}/sitemap.xml
`);
}

async function prerender() {
  console.log('Starting pre-rendering process...');

  const { posts, routes } = discoverContent();
  writeDiscoveryFiles(posts, routes);

  await new Promise((resolve, reject) => {
    server.once('error', reject);
    server.listen(preferredPort, () => {
      server.off('error', reject);
      resolve();
    });
  });

  const address = server.address();
  const port = typeof address === 'object' && address ? address.port : preferredPort;
  console.log(`Server running at http://localhost:${port}`);

  const browser = await chromium.launch({ headless: true });

  try {
    const context = await browser.newContext({
      viewport: { width: 1280, height: 720 }
    });

    await context.route('**/*', route => {
      const requestUrl = new URL(route.request().url());
      const isLocal = requestUrl.hostname === 'localhost' || requestUrl.hostname === '127.0.0.1';

      if (isLocal) {
        route.continue();
      } else {
        route.abort();
      }
    });

    const page = await context.newPage();

    console.log(`Total routes to pre-render: ${routes.length}`);

    for (const route of routes) {
      console.log(`Pre-rendering ${route}...`);

      await page.goto(`http://localhost:${port}${route}`, {
        waitUntil: 'domcontentloaded',
        timeout: 45000
      });

      await page.waitForSelector('#app h1, #app article, #app .mud-layout', { timeout: 20000 }).catch(() => {
        console.log(`Timed out waiting for app content on ${route}, continuing with current HTML.`);
      });

      await page.waitForFunction(() => {
        const diagrams = Array.from(document.querySelectorAll('.mermaid'));
        return diagrams.length === 0 || diagrams.every(diagram =>
          diagram.getAttribute('data-processed') === 'true' && diagram.querySelector('svg'));
      }, { timeout: 20000 });

      const notFoundText = await page.locator('[role="alert"]').textContent().catch(() => '');
      if (notFoundText?.includes("Sorry, there's nothing")) {
        throw new Error(`Route rendered NotFound: ${route}`);
      }

      const html = await page.content();
      const outputPath = route === '/'
        ? join(wwwroot, 'index.html')
        : join(wwwroot, route, 'index.html');

      mkdirSync(dirname(outputPath), { recursive: true });
      writeFileSync(outputPath, html);
      console.log(`Saved ${outputPath}`);
    }

    const indexHtml = readFileSync(join(wwwroot, 'index.html'), 'utf8');
    writeFileSync(join(wwwroot, '404.html'), indexHtml);
    console.log('Created 404.html for GitHub Pages');
  } finally {
    await browser.close();
    server.close();
    console.log('Pre-rendering complete.');
  }
}

prerender().catch(error => {
  console.error('Pre-rendering failed:', error);
  process.exit(1);
});
