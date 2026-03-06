import { defineEcConfig } from 'astro-expressive-code';
import gitignoreGrammar from './src/grammars/gitignore.tmLanguage.json' with { type: 'json' };
import promqlGrammar from './src/grammars/promql.tmLanguage.json' with { type: 'json' };
import traceqlGrammar from './src/grammars/traceql.tmLanguage.json' with { type: 'json' };
import logqlGrammar from './src/grammars/logql.tmLanguage.json' with { type: 'json' };

export default defineEcConfig({
  shiki: {
    langs: [gitignoreGrammar, promqlGrammar, traceqlGrammar, logqlGrammar],
  },
});
