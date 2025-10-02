import { QueryClient, QueryClientProvider } from '@tanstack/react-query';
import { render, screen } from '@testing-library/react';
import { vi, describe, it, beforeEach } from 'vitest';

import type { EditFaceDialogFace } from './EditFaceDialog';
import { EditFaceDialog } from './EditFaceDialog';

const mutateAsyncMock = vi.fn();

vi.mock('@photobank/shared/api/photobank', () => ({
  IdentityStatus: { Identified: 'Identified' },
  getFacesGetQueryKey: () => ['faces-get'],
  useFacesUpdate: () => ({
    mutateAsync: mutateAsyncMock,
    isPending: false,
  }),
  usePersonsGetAll: () => ({
    data: { data: [] },
    isLoading: false,
    isError: false,
    refetch: vi.fn(),
  }),
}));

describe('EditFaceDialog', () => {
  beforeEach(() => {
    mutateAsyncMock.mockReset();
  });

  const renderComponent = (face: EditFaceDialogFace) => {
    const queryClient = new QueryClient({
      defaultOptions: {
        queries: { retry: false },
        mutations: { retry: false },
      },
    });

    return render(
      <QueryClientProvider client={queryClient}>
        <EditFaceDialog open onOpenChange={() => {}} face={face} />
      </QueryClientProvider>
    );
  };

  it('renders the face preview image when an image URL is available', () => {
    const face: EditFaceDialogFace = {
      id: 12,
      faceId: 12,
      personId: 42,
      personName: 'Jane Doe',
      imageUrl: 'https://example.com/face.jpg',
      identityStatus: 'Identified',
    };

    renderComponent(face);

    expect(screen.getByAltText('Face preview for Jane Doe')).toBeInTheDocument();
  });
});
