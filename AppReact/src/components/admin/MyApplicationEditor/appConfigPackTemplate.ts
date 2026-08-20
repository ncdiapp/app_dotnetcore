const appConfigPackTemplate: Record<string, unknown> = {
  schemaVersion: 1,
  generatedAt: '2026-08-17T00:00:00Z',
  source: {
    generatedBy: 'manual',
    applicationName: 'Demo Order',
    notes: 'Pattern A — Search + MasterDetail. See sample-listedit.appConfigPack.json for ListEdit.'
  },
  tables: [
    {
      name: 'Demo_Order',
      schemaOwner: 'dbo',
      description: 'Order header',
      columns: [
        { name: 'OrderId', dataType: 'INT', isPrimaryKey: true, isNullable: false, isAutoIncrement: true },
        { name: 'OrderCode', dataType: 'NVARCHAR', length: 50, isNullable: false },
        { name: 'OrderDate', dataType: 'DATETIME', isNullable: true },
        { name: 'StatusId', dataType: 'INT', isNullable: true },
        { name: 'Notes', dataType: 'NVARCHAR', length: 500, isNullable: true }
      ],
      relationships: []
    },
    {
      name: 'Demo_OrderLine',
      schemaOwner: 'dbo',
      description: 'Order lines',
      columns: [
        { name: 'OrderLineId', dataType: 'INT', isPrimaryKey: true, isNullable: false, isAutoIncrement: true },
        { name: 'OrderId', dataType: 'INT', isNullable: false },
        { name: 'LineNo', dataType: 'INT', isNullable: true },
        { name: 'Sku', dataType: 'NVARCHAR', length: 80, isNullable: true },
        { name: 'Qty', dataType: 'DECIMAL', precision: 18, scale: 2, isNullable: true }
      ],
      relationships: [
        {
          type: 'MANY_TO_ONE',
          targetTable: 'Demo_Order',
          foreignKeyColumn: 'OrderId',
          referencedColumn: 'OrderId'
        }
      ]
    }
  ],
  views: [
    {
      name: 'View_Demo_OrderList',
      schemaOwner: 'dbo',
      createOrAlterSql:
        'CREATE OR ALTER VIEW dbo.View_Demo_OrderList AS SELECT o.OrderId, o.OrderCode, o.OrderDate, o.StatusId, o.Notes FROM dbo.Demo_Order AS o'
    }
  ],
  transactions: [
    {
      integrationId: 'TX_DemoOrder',
      name: 'Demo Order',
      description: 'Demo order edit',
      organizedType: 'MasterDetail',
      formMode: 'Default',
      unitStructure: {
        rootTableName: 'Demo_Order',
        siblingTableNames: [],
        childUnits: [
          { tableName: 'Demo_OrderLine', displayName: 'Order Lines', grandChildTableNames: [], gridDisplayType: 1 }
        ]
      },
      fields: [
        { tableName: 'Demo_Order', columnName: 'OrderCode', displayName: 'Order Code', controlType: 2, isVisible: true },
        { tableName: 'Demo_Order', columnName: 'OrderDate', displayName: 'Order Date', controlType: 7, isVisible: true },
        { tableName: 'Demo_Order', columnName: 'StatusId', displayName: 'Status', controlType: 2, isVisible: true },
        { tableName: 'Demo_OrderLine', columnName: 'Sku', displayName: 'SKU', controlType: 2, isVisible: true },
        { tableName: 'Demo_OrderLine', columnName: 'Qty', displayName: 'Qty', controlType: 20, isVisible: true }
      ]
    }
  ],
  transactionGroup: {
    name: 'Demo Order Template',
    integrationId: 'TG_DemoOrder',
    primaryTransactionIntegrationId: 'TX_DemoOrder',
    memberTransactionIntegrationIds: ['TX_DemoOrder']
  },
  searches: [
    {
      integrationId: 'Search_DemoOrder',
      name: 'Demo Order List',
      description: 'List of demo orders',
      usageType: 'Management',
      autoExecute: true,
      dataSet: {
        name: 'Demo Order List',
        primaryTableName: 'Demo_Order',
        queryText: 'SELECT OrderId, OrderCode, OrderDate, StatusId, Notes FROM dbo.View_Demo_OrderList'
      },
      criteriaFields: [
        {
          displayText: 'Order Code',
          sysTableFiledPath: 'OrderCode',
          controlType: 2,
          isVisible: true,
          sort: 10,
          positionRow: 1,
          positionColumn: 1
        }
      ],
      searchView: {
        name: 'Demo Order Grid',
        integrationId: 'Search_DemoOrder_View',
        gridOutputMode: 1,
        fields: [
          { displayText: 'Order Id', sysTableFiledPath: 'OrderId', controlType: 20, isTransRootId: true, isVisible: false, sort: 1 },
          { displayText: 'Order Code', sysTableFiledPath: 'OrderCode', controlType: 2, isTransRootId: false, isVisible: true, sort: 10 },
          { displayText: 'Order Date', sysTableFiledPath: 'OrderDate', controlType: 7, isTransRootId: false, isVisible: true, sort: 20 },
          { displayText: 'Notes', sysTableFiledPath: 'Notes', controlType: 2, isTransRootId: false, isVisible: true, sort: 30 }
        ]
      },
      linkTargets: [
        { name: 'Create', actionType: 'Create', transactionIntegrationId: 'TX_DemoOrder', sourceColumn: 'OrderId', sort: 1 },
        { name: 'Edit', actionType: 'Edit', transactionIntegrationId: 'TX_DemoOrder', sourceColumn: 'OrderId', sort: 2 }
      ],
      menu: {
        registerInMainMenu: true,
        menuTitle: 'Demo Orders',
        menuOrder: 100
      }
    }
  ]
};

export default appConfigPackTemplate;
