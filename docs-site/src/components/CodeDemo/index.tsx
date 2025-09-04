import React, { useState } from 'react';
import CodeBlock from '@theme/CodeBlock';
import clsx from 'clsx';
import styles from './styles.module.css';

interface CodeDemoProps {
  title?: string;
  language?: string;
  code: string;
  showLineNumbers?: boolean;
  highlightLines?: number[];
  description?: string;
}

export default function CodeDemo({
  title,
  language = 'typescript',
  code,
  showLineNumbers = true,
  highlightLines = [],
  description
}: CodeDemoProps): React.ReactElement {
  const [copied, setCopied] = useState(false);

  const handleCopy = async () => {
    try {
      await navigator.clipboard.writeText(code);
      setCopied(true);
      setTimeout(() => setCopied(false), 2000);
    } catch (err) {
      console.error('Failed to copy:', err);
    }
  };

  return (
    <div className={styles.codeDemo}>
      {(title || description) && (
        <div className={styles.codeDemoHeader}>
          {title && <h4 className={styles.codeDemoTitle}>{title}</h4>}
          {description && <p className={styles.codeDemoDescription}>{description}</p>}
        </div>
      )}
      <div className={styles.codeDemoContent}>
        <div className={styles.codeActions}>
          <span className={styles.codeLanguage}>{language}</span>
          <button 
            className={clsx(styles.copyButton, copied && styles.copied)}
            onClick={handleCopy}
            aria-label="Copy code"
          >
            {copied ? (
              <>✓ Copied!</>
            ) : (
              <>📋 Copy</>
            )}
          </button>
        </div>
        <CodeBlock
          language={language}
          showLineNumbers={showLineNumbers}
          metastring={highlightLines.length > 0 ? `{${highlightLines.join(',')}}` : ''}
        >
          {code}
        </CodeBlock>
      </div>
    </div>
  );
}
