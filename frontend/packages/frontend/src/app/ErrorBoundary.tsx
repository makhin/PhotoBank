import { Component, type ErrorInfo, type ReactNode } from 'react';

import { Button } from '@/shared/ui/button';

interface Props {
  children: ReactNode;
}

interface State {
  hasError: boolean;
}

export default class ErrorBoundary extends Component<Props, State> {
  public state: State = { hasError: false };

  static getDerivedStateFromError(): State {
    return { hasError: true };
  }

  componentDidCatch(error: Error, errorInfo: ErrorInfo) {
    console.error('Uncaught error:', error, errorInfo);
  }

  private handleRetry = () => {
    this.setState({ hasError: false });
  };

  render() {
    if (this.state.hasError) {
      return (
        <div className="p-6 space-y-4">
          <p className="text-muted-foreground">Произошла ошибка.</p>
          <Button onClick={this.handleRetry}>Повторить</Button>
        </div>
      );
    }

    return this.props.children;
  }
}

