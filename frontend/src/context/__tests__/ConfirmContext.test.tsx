import { useState } from 'react'
import { describe, expect, it, vi } from 'vitest'
import { render, screen } from '@testing-library/react'
import userEvent from '@testing-library/user-event'
import { ConfirmProvider, useConfirm } from '../ConfirmContext'

// A tiny harness component so we can exercise useConfirm() the way real
// callers do — call it, await the result, and show what it resolved to.
function Harness() {
  const confirm = useConfirm()
  const [result, setResult] = useState<string>('idle')

  const handleClick = async () => {
    const ok = await confirm({ title: 'Delete this thing?', confirmLabel: 'Delete', danger: true })
    setResult(ok ? 'confirmed' : 'cancelled')
  }

  return (
    <div>
      <button onClick={handleClick}>Trigger</button>
      <p>Result: {result}</p>
    </div>
  )
}

describe('ConfirmContext', () => {
  it('resolves true when the confirm button is clicked', async () => {
    const user = userEvent.setup()
    render(
      <ConfirmProvider>
        <Harness />
      </ConfirmProvider>,
    )

    await user.click(screen.getByText('Trigger'))
    expect(screen.getByText('Delete this thing?')).toBeInTheDocument()

    await user.click(screen.getByRole('button', { name: 'Delete' }))

    expect(await screen.findByText('Result: confirmed')).toBeInTheDocument()
    expect(screen.queryByText('Delete this thing?')).not.toBeInTheDocument()
  })

  it('resolves false when Cancel is clicked', async () => {
    const user = userEvent.setup()
    render(
      <ConfirmProvider>
        <Harness />
      </ConfirmProvider>,
    )

    await user.click(screen.getByText('Trigger'))
    await user.click(screen.getByRole('button', { name: 'Cancel' }))

    expect(await screen.findByText('Result: cancelled')).toBeInTheDocument()
  })

  it('resolves false when the overlay is clicked outside the dialog', async () => {
    const user = userEvent.setup()
    const { container } = render(
      <ConfirmProvider>
        <Harness />
      </ConfirmProvider>,
    )

    await user.click(screen.getByText('Trigger'))
    const overlay = container.querySelector('.modal-overlay')
    expect(overlay).not.toBeNull()
    await user.click(overlay as Element)

    expect(await screen.findByText('Result: cancelled')).toBeInTheDocument()
  })

  it('throws if useConfirm is called outside a ConfirmProvider', () => {
    // Suppress the expected React error-boundary console noise for this one case.
    const spy = vi.spyOn(console, 'error').mockImplementation(() => {})
    expect(() => render(<Harness />)).toThrow('useConfirm must be used within a ConfirmProvider')
    spy.mockRestore()
  })
})
