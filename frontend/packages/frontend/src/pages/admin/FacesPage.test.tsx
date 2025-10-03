import { render, screen, waitFor, within } from '@testing-library/react';
import userEvent from '@testing-library/user-event';
import { QueryClient, QueryClientProvider } from '@tanstack/react-query';
import React from 'react';

import { IdentityStatusDto as IdentityStatus } from '@photobank/shared/api/photobank';

import FacesPage from './FacesPage';

beforeAll(() => {
  if (!Element.prototype.hasPointerCapture) {
    Element.prototype.hasPointerCapture = () => false;
  }
  if (!Element.prototype.setPointerCapture) {
    Element.prototype.setPointerCapture = () => {};
  }
  if (!Element.prototype.releasePointerCapture) {
    Element.prototype.releasePointerCapture = () => {};
  }
  if (!Element.prototype.scrollIntoView) {
    Element.prototype.scrollIntoView = () => {};
  }
});

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
    IdentityStatus: (actual as { IdentityStatus?: unknown; IdentityStatusDto?: unknown }).IdentityStatus ?? (actual as { IdentityStatusDto?: unknown }).IdentityStatusDto,
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

  test('normalises numeric identity statuses before rendering', async () => {
    const face = {
      id: 7,
      faceId: 7,
      identityStatus: 3,
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

    expect(await screen.findByText('Identified')).toBeInTheDocument();
    expect(screen.getByText('#7')).toBeInTheDocument();
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

  test('shows backend identity statuses in the edit dialog', async () => {
    const mutateAsync = vi.fn().mockResolvedValue({});
    const face = {
      id: 99,
      faceId: 99,
      identityStatus: IdentityStatus.Identified,
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
    const user = userEvent.setup();

    render(<FacesPage />, { wrapper: Wrapper });

    await user.click(await screen.findByRole('button', { name: /edit/i }));

    const statusField = screen.getByText('Identity Status').closest('div');
    expect(statusField).not.toBeNull();

    const statusTrigger = statusField && within(statusField).getByRole('combobox');
    expect(statusTrigger).toBeTruthy();

    await user.click(statusTrigger!);

    const backendStatuses = Object.values(IdentityStatus);
    const formattedStatuses = backendStatuses.map((status) =>
      status.replace(/([a-z0-9])([A-Z])/g, '$1 $2')
    );

    const listbox = await screen.findByRole('listbox');

    for (const status of formattedStatuses) {
      expect(await within(listbox).findByText(status)).toBeInTheDocument();
    }
  });

  test('renders identity status badges with backend colour mapping', async () => {
    const statuses = Object.values(IdentityStatus);
    const faces = statuses.map((status, index) => ({
      id: index + 1,
      faceId: index + 1,
      identityStatus: status,
    }));

    mockUseFacesGet.mockReturnValue({
      data: { data: faces },
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

    const expectedClasses: Record<IdentityStatus, string> = {
      [IdentityStatus.Identified]: 'bg-success text-success-foreground',
      [IdentityStatus.ForReprocessing]: 'bg-warning text-warning-foreground',
      [IdentityStatus.NotDetected]: 'bg-warning text-warning-foreground',
      [IdentityStatus.NotIdentified]: 'bg-warning text-warning-foreground',
      [IdentityStatus.StopProcessing]: 'bg-destructive text-destructive-foreground',
      [IdentityStatus.Undefined]: 'bg-muted text-muted-foreground',
    };

    for (const status of statuses) {
      const label = status.replace(/([a-z0-9])([A-Z])/g, '$1 $2');
      const badge = await screen.findByText(label);
      const expectedClass = expectedClasses[status];

      if (!expectedClass) {
        throw new Error(`Missing expected class for status: ${status}`);
      }

      expect(badge).toHaveClass(expectedClass);
    }
  });
});
