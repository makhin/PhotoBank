import { QueryClient, QueryClientProvider } from '@tanstack/react-query';
import { render, screen } from '@testing-library/react';
import { vi, describe, it, beforeEach, expect } from 'vitest';

import { EditFaceDialog } from './EditFaceDialog';
import type { FaceDto } from '@photobank/shared';

const mutateAsyncMock = vi.hoisted(() => vi.fn());
const useFacesGetImageMock = vi.hoisted(() => vi.fn());

vi.mock('@photobank/shared/api/photobank', async () => {
  const actual = await vi.importActual<
    typeof import('@photobank/shared/api/photobank')
  >('@photobank/shared/api/photobank');

  return {
    ...actual,
    IdentityStatus: { Identified: 'Identified' },
    getFacesGetQueryKey: () => ['faces-get'],
    useFacesUpdate: () => ({
      mutateAsync: mutateAsyncMock,
      isPending: false,
    }),
    usePersonsGetAll: () => ({
      data: {
        data: [
          { id: 42, name: 'Jane Doe' },
          { id: 30, name: 'John Smith' },
        ],
      },
      isLoading: false,
      isError: false,
      refetch: vi.fn(),
    }),
    useFacesGetImage: useFacesGetImageMock,
  };
});

describe('EditFaceDialog', () => {
  beforeEach(() => {
    mutateAsyncMock.mockReset();
    useFacesGetImageMock.mockReset();
    useFacesGetImageMock.mockReturnValue({ data: null });
  });

  const renderComponent = (face: FaceDto) => {
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
    const face: FaceDto = {
      id: 12,
      personId: 42,
      personName: 'Jane Doe',
      imageUrl: 'https://example.com/face.jpg',
      identityStatus: 'Identified',
    };

    renderComponent(face);

    expect(screen.getByAltText('Face preview for Jane Doe')).toBeInTheDocument();
  });

  it('uses the face image URL from the API when provided', () => {
    const apiImageUrl = 'https://example.com/from-api.jpg';

    useFacesGetImageMock.mockReturnValue({
      data: {
        data: null,
        status: 301,
        headers: new Headers({ Location: apiImageUrl }),
      },
    });

    const face: FaceDto = {
      id: 15,
      personId: 30,
      personName: 'John Smith',
      identityStatus: 'Identified',
    };

    renderComponent(face);

    expect(screen.getByAltText('Face preview for John Smith')).toHaveAttribute(
      'src',
      apiImageUrl
    );
  });
});
