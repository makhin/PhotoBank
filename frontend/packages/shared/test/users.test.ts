import { beforeEach, describe, expect, it, vi } from 'vitest';

describe('admin users api', () => {
  beforeEach(() => {
    vi.resetModules();
  });

  it('getAllUsers fetches users', async () => {
    const getMock = vi.fn().mockResolvedValue({ data: [{ id: '1' }] });
    vi.doMock('../src/api/client', () => ({ apiClient: { get: getMock } }));
    const { getAllUsers } = await import('../src/api/users');
    const res = await getAllUsers();
    expect(getMock).toHaveBeenCalledWith('/admin/users');
    expect(res).toEqual([{ id: '1' }]);
  });

  it('updateUserById sends data', async () => {
    const putMock = vi.fn().mockResolvedValue({});
    vi.doMock('../src/api/client', () => ({ apiClient: { put: putMock } }));
    const { updateUserById } = await import('../src/api/users');
    await updateUserById('5', { phoneNumber: '1' });
    expect(putMock).toHaveBeenCalledWith('/admin/users/5', { phoneNumber: '1' });
  });

  it('setUserClaims posts claims', async () => {
    const putMock = vi.fn().mockResolvedValue({});
    vi.doMock('../src/api/client', () => ({ apiClient: { put: putMock } }));
    const { setUserClaims } = await import('../src/api/users');
    await setUserClaims('3', [{ type: 't', value: 'v' }]);
    expect(putMock).toHaveBeenCalledWith('/admin/users/3/claims', [
      { type: 't', value: 'v' },
    ]);
  });
});
