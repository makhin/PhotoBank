import { render, screen, waitFor } from '@testing-library/react';
import userEvent from '@testing-library/user-event';
import { QueryClient, QueryClientProvider } from '@tanstack/react-query';
import React from 'react';

import FacesPage from './FacesPage';

const { mockUseFacesGet, mockUsePersonsGetAll, mockUseFacesUpdate, mockToast } = vi.hoisted(() => ({
  mockUseFacesGet: vi.fn(),
  mockUsePersonsGetAll: vi.fn(),
  mockUseFacesUpdate: vi.fn(),
  mockToast: vi.fn(),
}));

vi.mock('@photobank/shared/api/photobank', async () => {
  const actual = await vi.importActual<typeof import('@photobank/shared/api/photobank')>(
    '@photobank/shared/api/photobank'
  );

  return {
    ...actual,
    useFacesGet: mockUseFacesGet,
    usePersonsGetAll: mockUsePersonsGetAll,
    useFacesUpdate: mockUseFacesUpdate,
  };
});

vi.mock('@/hooks/use-toast', () => ({
  useToast: () => ({ toast: mockToast }),
}));

const createWrapper = () => {
  const queryClient = new QueryClient({
    defaultOptions: {
      queries: {
        retry: false,
      },
    },
  });

  const Wrapper: React.FC<React.PropsWithChildren> = ({ children }) => (
    <QueryClientProvider client={queryClient}>{children}</QueryClientProvider>
  );

  return { Wrapper };
};

describe('FacesPage', () => {
  afterEach(() => {
    vi.clearAllMocks();
  });

  test('renders an unassigned face from the /faces endpoint', async () => {
    const face = {
      id: 123,
      faceId: 123,
      identityStatus: 'NotIdentified',
      imageUrl: null,
    };

    mockUseFacesGet.mockReturnValue({
      data: { data: [face] },
      isLoading: false,
      isError: false,
      isFetching: false,
      refetch: vi.fn(),
    });

    mockUsePersonsGetAll.mockReturnValue({
      data: { data: [] },
      isLoading: false,
      isError: false,
      refetch: vi.fn(),
    });

    mockUseFacesUpdate.mockReturnValue({
      mutateAsync: vi.fn(),
      isPending: false,
    });

    const { Wrapper } = createWrapper();

    render(<FacesPage />, { wrapper: Wrapper });

    expect(await screen.findByText('Unassigned')).toBeInTheDocument();
    expect(screen.getByText('#123')).toBeInTheDocument();
  });

  test('allows editing and unassigning a face without a person', async () => {
    const mutateAsync = vi.fn().mockResolvedValue({});
    const face = {
      id: 42,
      faceId: 42,
      identityStatus: 'NotIdentified',
    };

    mockUseFacesGet.mockReturnValue({
      data: { data: [face] },
      isLoading: false,
      isError: false,
      isFetching: false,
      refetch: vi.fn(),
    });

    mockUsePersonsGetAll.mockReturnValue({
      data: { data: [] },
      isLoading: false,
      isError: false,
      refetch: vi.fn(),
    });

    mockUseFacesUpdate.mockReturnValue({
      mutateAsync,
      isPending: false,
    });

    const { Wrapper } = createWrapper();

    render(<FacesPage />, { wrapper: Wrapper });

    const user = userEvent.setup();

    await user.click(await screen.findByRole('button', { name: /edit/i }));

    expect(await screen.findByRole('dialog')).toBeInTheDocument();

    await user.click(screen.getByRole('button', { name: /save changes/i }));

    await waitFor(() => {
      expect(mutateAsync).toHaveBeenCalledWith({
        data: {
          faceId: 42,
          personId: null,
          identityStatus: 'NotIdentified',
        },
      });
    });

    expect(mockToast).toHaveBeenCalledWith(
      expect.objectContaining({ title: 'Face unassigned' })
    );
  });
});
