import { StrictMode } from 'react'
import { createRoot } from 'react-dom/client'
import { createBrowserRouter } from 'react-router-dom';
import { RouterProvider } from 'react-router';
import OverviewPage from './pages/Home/OverviewPage.tsx';
import EditItemsPage from './pages/Items/EditItemsPage.tsx';
import SalesPage from './pages/Sales/SalesPage.tsx';
import CreateSalesPage from './pages/Sales/CreateSalesPage.tsx';
import EditSalesPage from './pages/Sales/EditSalesPage.tsx';
import ItemsPage from './pages/Items/ItemsPage.tsx';
import CreateItemsPage from './pages/Items/CreateItemsPage.tsx';

const router = createBrowserRouter([
  {
    path: "/",
    element: <OverviewPage />,
  },
  {
    path: "/items",
    element: <ItemsPage />,
  },
  {
    path: "/items/create",
    element: <CreateItemsPage />,
  },
  {
    path: "/items/edit/:id",
    element: <EditItemsPage />,
  },
  {
    path: "/sales",
    element: <SalesPage />,
  },
  {
    path: "/sales/create",
    element: <CreateSalesPage />,
  },
  {
    path: "/sales/edit/:id",
    element: <EditSalesPage />,
  },
]);



createRoot(document.getElementById('root')!).render(
 <StrictMode>
    <RouterProvider router={router} />
  </StrictMode>,
)
