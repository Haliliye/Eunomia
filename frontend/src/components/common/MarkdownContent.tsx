import { useMemo } from 'react'
import { marked } from 'marked'
import DOMPurify from 'dompurify'

interface MarkdownContentProps {
  content: string
}

// Renders bold/italic/lists/headings/code blocks/links in story descriptions
// and comments. marked's output is sanitized through DOMPurify before it
// ever reaches dangerouslySetInnerHTML — markdown coming from another
// team member is still untrusted input.
export default function MarkdownContent({ content }: MarkdownContentProps) {
  const html = useMemo(() => {
    const rawHtml = marked.parse(content, { async: false, breaks: true }) as string
    return DOMPurify.sanitize(rawHtml)
  }, [content])

  return <div className="markdown-content" dangerouslySetInnerHTML={{ __html: html }} />
}
